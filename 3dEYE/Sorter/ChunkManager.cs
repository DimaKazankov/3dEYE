using System.Text;
using _3dEYE.Sorter.Models;
using Microsoft.Extensions.Logging;

namespace _3dEYE.Sorter;

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
        var currentPosition = 0L;
        
        // Calculate target lines per chunk based on buffer size
        // Assume average line is ~100 characters, but adjust based on actual buffer size
        var targetLinesPerChunk = Math.Max(1000, bufferSize / 200); // At least 1000 lines per chunk
        
        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line == null) break;
            
            // Create LineData with position tracking
            var lineData = LineData.FromString(line, currentPosition);
            lines.Add(lineData);
            currentPosition = reader.BaseStream.Position;
            
            // If we've accumulated enough lines to fill our buffer, sort and write chunk
            if (lines.Count >= targetLinesPerChunk)
            {
                var chunkFile = await WriteChunkAsync(lines, tempDirectory, chunkIndex++, comparer, cancellationToken).ConfigureAwait(false);
                chunkFiles.Add(chunkFile);
                lines.Clear();
            }
        }
        
        // Write remaining lines as final chunk
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
        
        // Write sorted lines efficiently
        foreach (var lineData in lines)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // Use the content directly for efficient writing
            await writer.WriteLineAsync(lineData.Content, cancellationToken).ConfigureAwait(false);
        }
        
        return chunkFilePath;
    }

    public async IAsyncEnumerable<LineData> ReadChunkAsync(
        string chunkFilePath, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(chunkFilePath, Encoding.UTF8, true, bufferSize);
        var position = 0L;
        
        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line == null) break;
            
            var lineData = LineData.FromString(line, position);
            position = reader.BaseStream.Position;
            
            yield return lineData;
        }
    }

    public int EstimateChunkCount(long fileSizeBytes)
    {
        // Rough estimate: assume average line is 100 characters (200 bytes in UTF-8)
        var estimatedLines = fileSizeBytes / 200;
        var linesPerChunk = bufferSize / 100;
        return (int)Math.Ceiling((double)estimatedLines / linesPerChunk);
    }

    public static void CleanupChunks(IEnumerable<string> chunkFiles, ILogger logger)
    {
        foreach (var chunkFile in chunkFiles)
        {
            try
            {
                if (File.Exists(chunkFile))
                {
                    File.Delete(chunkFile);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning("Failed to delete chunk file {ChunkFile}: {Message}", chunkFile, ex.Message);
            }
        }
    }
} 