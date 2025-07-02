using System.Buffers;
using System.Globalization;
using System.IO.Pipelines;

namespace _3dEYE.Sorter;

internal class FileChunkProcessor
{
    public async Task<IReadOnlyList<string>> SplitToChunksAsync(
        string inputPath,
        string tempDir,
        int chunkChars = 32 * 1024 * 1024,
        int maxLineChars = 1024,
        bool tolerant = true) // skip bad lines vs. throw
    {
        if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

        var runs = new List<string>();
        var pipe = PipeReader.Create(File.OpenRead(inputPath), new StreamPipeReaderOptions(bufferSize: 1 << 20));
        var buffer = ArrayPool<char>.Shared.Rent(chunkChars + maxLineChars);
        var used = 0; // chars currently in buffer
        var runIdx = 0;

        try
        {
            while (true)
            {
                used += await pipe.FillBufferUtf8Async(buffer.AsMemory(used, chunkChars - used));
                if (used == 0) break; // EOF

                var cut = buffer.AsSpan(0, used).LastLf(); // ensures full lines only
                ReadOnlySpan<char> chunk = buffer.AsSpan(0, cut);

                // move spill to front
                var spill = used - cut;
                if (spill > 0) buffer.AsSpan(cut, spill).CopyTo(buffer);
                used = spill;

                var entries = new List<LineEntry>(chunk.Length / 32);
                var pos = 0;
                while (pos < chunk.Length)
                {
                    var lf = chunk.Slice(pos).IndexOf('\n');
                    if (lf < 0) break; // safety
                    var line = chunk.Slice(pos, lf);
                    pos += lf + 1;

                    if (!line.IsEmpty && line[^1] == '\r') line = line[..^1];

                    var dot = line.IndexOf('.');
                    var spRel = dot >= 0 ? line[(dot + 1)..].IndexOf(' ') : -1;
                    var sp = spRel >= 0 ? spRel + dot + 1 : -1;

                    if (dot < 0 || sp <= dot)
                    {
                        if (tolerant) continue;
                        throw new FormatException($"Malformed line: \"{line.ToString()}\"");
                    }

                    if (!int.TryParse(line[..dot], NumberStyles.None, CultureInfo.InvariantCulture, out var num))
                    {
                        if (tolerant) continue;
                        throw new FormatException($"Bad number: {line[..dot].ToString()}");
                    }

                    var full = buffer.AsMemory(pos - lf - 1, lf + 1);
                    var key = buffer.AsMemory(pos - lf - 1 + sp + 1, line.Length - sp - 1);
                    entries.Add(new LineEntry(full, key, num));
                }

                entries.Sort(); // uses LineEntry.CompareTo

                var runPath = Path.Combine(tempDir, $"run_{runIdx++:D4}.txt");
                await entries.FlushRunAsync(runPath);
                runs.Add(runPath);
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }

        return runs;
    }
}