using System.Buffers;
using System.Globalization;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;

namespace _3dEYE.Sorter;

/// <summary>Immutable view over one input line during the split phase.</summary>
internal readonly struct LineEntry : IComparable<LineEntry>
{
    public readonly ReadOnlyMemory<char> FullLine; // "<num>. <text>\n"
    public readonly ReadOnlyMemory<char> KeyText;  // "<text>"
    public readonly int Number;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LineEntry(ReadOnlyMemory<char> full, ReadOnlyMemory<char> key, int number)
        => (FullLine, KeyText, Number) = (full, key, number);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(LineEntry other)
    {
        int s = KeyText.Span.SequenceCompareTo(other.KeyText.Span);
        return s != 0 ? s : Number.CompareTo(other.Number);
    }
}

/// <summary>Queue element used during k‑way merge.</summary>
readonly struct MergeEntry
{
    public readonly string Line;       // full line WITHOUT '\n'
    public readonly int    KeyStart;   // index where the key starts
    public readonly int    KeyLen;     // length (can be 0)
    public readonly int    Number;     // numeric prefix
    public readonly int    RunIdx;     // origin run

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MergeEntry(string line, int keyStart, int keyLen, int number, int runIdx)
        => (Line, KeyStart, KeyLen, Number, RunIdx) = (line, keyStart, keyLen, number, runIdx);

    public ReadOnlySpan<char> KeySpan => Line.AsSpan(KeyStart, KeyLen);
}

file sealed class MergeEntryComparer : IComparer<MergeEntry>
{
    public static readonly MergeEntryComparer Instance = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Compare(MergeEntry x, MergeEntry y)
    {
        int s = x.KeySpan.SequenceCompareTo(y.KeySpan);
        return s != 0 ? s : x.Number.CompareTo(y.Number);
    }
}

#region  ───────────  Split‑and‑sort worker  ───────────

internal static class SplitSortWorker
{
    private static readonly Decoder Utf8Decoder = Encoding.UTF8.GetDecoder();

    /// <summary>
    /// Streams <paramref name="inputPath"/> and emits individually‑sorted *run* files under
    /// <paramref name="tempDir"/>.  Returns the list of run paths.
    /// </summary>
    public static async Task<IReadOnlyList<string>> SplitIntoRunsAsync(
        string inputPath,
        string tempDir,
        int    chunkChars   = 32 * 1024 * 1024, // 64 MiB UTF‑16
        int    maxLineChars = 1024,
        bool   tolerant     = true)             // skip bad lines vs. throw
    {
        if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

        var runs      = new List<string>();
        var pipe      = PipeReader.Create(File.OpenRead(inputPath), new StreamPipeReaderOptions(bufferSize: 1 << 20));
        char[] buffer = ArrayPool<char>.Shared.Rent(chunkChars + maxLineChars);
        int used      = 0;   // chars currently in buffer
        int runIdx    = 0;

        try
        {
            while (true)
            {
                used += await FillBufferUtf8Async(pipe, buffer.AsMemory(used, chunkChars - used));
                if (used == 0) break; // EOF

                int cut = LastLf(buffer.AsSpan(0, used));       // ensures full lines only
                ReadOnlySpan<char> chunk = buffer.AsSpan(0, cut);

                // move spill to front
                int spill = used - cut;
                if (spill > 0) buffer.AsSpan(cut, spill).CopyTo(buffer);
                used = spill;

                var entries = new List<LineEntry>(chunk.Length / 32);
                int pos = 0;
                while (pos < chunk.Length)
                {
                    int lf = chunk.Slice(pos).IndexOf('\n');
                    if (lf < 0) break;                  // safety
                    ReadOnlySpan<char> line = chunk.Slice(pos, lf);
                    pos += lf + 1;

                    if (!line.IsEmpty && line[^1] == '\r') line = line[..^1];

                    int dot = line.IndexOf('.');
                    int spRel = dot >= 0 ? line[(dot + 1)..].IndexOf(' ') : -1;
                    int sp   = spRel >= 0 ? spRel + dot + 1 : -1;

                    if (dot < 0 || sp <= dot) { if (tolerant) continue; throw new FormatException($"Malformed line: \"{line.ToString()}\""); }

                    if (!int.TryParse(line[..dot], NumberStyles.None, CultureInfo.InvariantCulture, out int num))
                    { if (tolerant) continue; throw new FormatException($"Bad number: {line[..dot].ToString()}"); }

                    var full = buffer.AsMemory(pos - lf - 1, lf + 1);
                    var key  = buffer.AsMemory(pos - lf - 1 + sp + 1, line.Length - sp - 1);
                    entries.Add(new LineEntry(full, key, num));
                }

                entries.Sort(); // uses LineEntry.CompareTo

                string runPath = Path.Combine(tempDir, $"run_{runIdx++:D4}.txt");
                await FlushRunAsync(entries, runPath);
                runs.Add(runPath);
            }
        }
        finally { ArrayPool<char>.Shared.Return(buffer); }

        return runs;
    }

    // --------------------- helpers ---------------------

    private static async Task<int> FillBufferUtf8Async(PipeReader reader, Memory<char> dest)
    {
        int written = 0;
        while (written < dest.Length)
        {
            ReadResult rr = await reader.ReadAsync();
            ReadOnlySequence<byte> seq = rr.Buffer;
            if (seq.IsEmpty && rr.IsCompleted) break;

            SequencePosition consumed = seq.Start;
            while (seq.Length > 0 && written < dest.Length)
            {
                ReadOnlySpan<byte> bytes = seq.FirstSpan;
                Span<char> chars        = dest.Span.Slice(written);

                Utf8Decoder.Convert(bytes, chars, flush: rr.IsCompleted, out int bytesUsed, out int charsUsed, out _);

                written   += charsUsed;
                consumed   = seq.GetPosition(bytesUsed, consumed);
                seq        = seq.Slice(consumed);
            }

            reader.AdvanceTo(consumed);
            if (rr.IsCompleted || written == dest.Length) break;
        }
        return written;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int LastLf(ReadOnlySpan<char> span)
    {
        int i = span.LastIndexOf('\n');
        if (i < 0) throw new InvalidDataException("Input line longer than the configured maxLineChars");
        return i + 1;
    }

    private static async Task FlushRunAsync(IEnumerable<LineEntry> entries, string path)
    {
        await using var w = new StreamWriter(path, append: false, Encoding.UTF8, bufferSize: 1 << 20);
        foreach (var e in entries) await w.WriteAsync(e.FullLine);
    }
}

#endregion


internal static class MergeRunsWorker
{
    /// <summary>
    /// Performs a K‑way merge on already‑sorted run files and streams the final
    /// result to <paramref name="outputPath"/>.
    /// </summary>
    public static async Task MergeRunsAsync(IEnumerable<string> runPaths, string outputPath, bool tolerant = true)
    {
        // open all run streams
        var readers = new List<StreamReader>();
        foreach (string p in runPaths)
            readers.Add(new StreamReader(p, Encoding.UTF8, false, 1 << 16));

        var pq = new PriorityQueue<MergeEntry, MergeEntry>(MergeEntryComparer.Instance);

        for (int i = 0; i < readers.Count; i++)
            if (!await TryEnqueueAsync(readers[i], i, pq, tolerant))
                readers[i].Dispose();

        await using var writer = new StreamWriter(outputPath, append: false, Encoding.UTF8, 1 << 20);

        while (pq.TryDequeue(out var entry, out _))
        {
            await writer.WriteLineAsync(entry.Line);
            await TryEnqueueAsync(readers[entry.RunIdx], entry.RunIdx, pq, tolerant);
        }

        foreach (var r in readers) r.Dispose();
    }

    private static async ValueTask<bool> TryEnqueueAsync(StreamReader reader, int runIdx,
        PriorityQueue<MergeEntry, MergeEntry> pq, bool tolerant)
    {
        string? line = await reader.ReadLineAsync();
        if (line is null) return false; // EOF

        int dot = line.IndexOf('.');
        if (dot < 0 || dot + 1 >= line.Length || line[dot + 1] != ' ')
        {
            if (tolerant) return true;  // skip
            throw new FormatException($"Bad line: \"{line}\"");
        }

        if (!int.TryParse(line.AsSpan(0, dot), NumberStyles.None, CultureInfo.InvariantCulture, out int num))
        {
            if (tolerant) return true;
            throw new FormatException($"Bad number: {line}");
        }

        int keyStart = dot + 2;               // may equal line.Length → empty key
        int keyLen   = line.Length - keyStart;

        var entry = new MergeEntry(line, keyStart, keyLen, num, runIdx);
        pq.Enqueue(entry, entry);
        return true;
    }
}
public class ThreeDEyeSorter
{
    public async Task SortAsync(string input, string output, string temp)
    {
        var runFiles = await SplitSortWorker.SplitIntoRunsAsync(input, temp); // phase 1
        await MergeRunsWorker.MergeRunsAsync(runFiles, output);
    }
}