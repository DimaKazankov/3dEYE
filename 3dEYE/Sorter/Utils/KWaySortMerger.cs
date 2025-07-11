using System.Globalization;
using System.Text;
using _3dEYE.Sorter.Models;

namespace _3dEYE.Sorter.Utils;

internal class KWaySortMerger(IComparer<MergeEntry>? comparer)
{
    private readonly IComparer<MergeEntry> _comparer = comparer ?? MergeEntryComparer.Instance;

    public async Task MergeAsync(IEnumerable<string> runPaths, string outputPath, bool tolerant = true)
    {
        var readers = runPaths.Select(p => new StreamReader(p, Encoding.UTF8, false, 1 << 16)).ToList();
        var priorityQueue = new PriorityQueue<MergeEntry, MergeEntry>(_comparer);

        for (var i = 0; i < readers.Count; i++)
            if (!await TryEnqueueAsync(readers[i], i, priorityQueue, tolerant))
                readers[i].Dispose();

        await using var writer = new StreamWriter(outputPath, append: false, Encoding.UTF8, 1 << 20);

        while (priorityQueue.TryDequeue(out var entry, out _))
        {
            await writer.WriteLineAsync(entry.Line);
            await TryEnqueueAsync(readers[entry.RunIdx], entry.RunIdx, priorityQueue, tolerant);
        }

        foreach (var r in readers) r.Dispose();
    }

    private static async ValueTask<bool> TryEnqueueAsync(StreamReader reader, int runIdx,
        PriorityQueue<MergeEntry, MergeEntry> pq, bool tolerant)
    {
        var line = await reader.ReadLineAsync();
        if (line is null) return false; // EOF

        var dot = line.IndexOf('.');
        if (dot < 0 || dot + 1 >= line.Length || line[dot + 1] != ' ')
        {
            if (tolerant) return true;  // skip
            throw new FormatException($"Bad line: \"{line}\"");
        }

        if (!int.TryParse(line.AsSpan(0, dot), NumberStyles.None, CultureInfo.InvariantCulture, out var num))
        {
            if (tolerant) return true;
            throw new FormatException($"Bad number: {line}");
        }

        var keyStart = dot + 2;
        var keyLen   = line.Length - keyStart;

        var entry = new MergeEntry(line, keyStart, keyLen, num, runIdx);
        pq.Enqueue(entry, entry);
        return true;
    }
}