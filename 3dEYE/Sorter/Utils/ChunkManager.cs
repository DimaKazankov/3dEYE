using System.Text;
using _3dEYE.Sorter.Models;

namespace _3dEYE.Sorter.Utils;

public class ChunkManager(int bufferSize = 1024 * 1024)
{
    public async Task<List<string>> SplitIntoChunksAsync(
        string inputFilePath, 
        string tempDirectory, 
        IComparer<LineData> comparer,
        CancellationToken cancellationToken = default)
    {
        var chunkFiles = new List<string>();
        var chunkIndex = 0;
        
        using var reader = new StreamReader(inputFilePath, Encoding.UTF8, true, bufferSize);
        var lines = new List<LineData>();

        var targetLinesPerChunk = Math.Max(1000, bufferSize / 200); // At least 1000 lines per chunk
        
        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line == null) break;
            
            var lineData = LineData.FromString(line);
            lines.Add(lineData);

            if (lines.Count >= targetLinesPerChunk)
            {
                var chunkFile = await WriteChunkAsync(lines, tempDirectory, chunkIndex++, comparer, cancellationToken).ConfigureAwait(false);
                chunkFiles.Add(chunkFile);
                lines.Clear();
            }
        }
        
        if (lines.Count > 0)
        {
            var chunkFile = await WriteChunkAsync(lines, tempDirectory, chunkIndex, comparer, cancellationToken).ConfigureAwait(false);
            chunkFiles.Add(chunkFile);
        }
        
        return chunkFiles;
    }

    private async Task<string> WriteChunkAsync(
        List<LineData> lines, 
        string tempDirectory, 
        int chunkIndex, 
        IComparer<LineData> comparer,
        CancellationToken cancellationToken)
    {
        lines.Sort(comparer);
        
        var chunkFilePath = Path.Combine(tempDirectory, $"chunk_{chunkIndex:D6}.tmp");

        await using var writer = new StreamWriter(chunkFilePath, false, Encoding.UTF8, bufferSize);
        
        foreach (var lineData in lines)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(lineData.Content, cancellationToken).ConfigureAwait(false);
        }
        
        return chunkFilePath;
    }

    public static void CleanupChunks(IEnumerable<string> chunkFiles)
    {
        foreach (var chunkFile in chunkFiles)
        {
            if (File.Exists(chunkFile))
                File.Delete(chunkFile);
        }
    }
} 