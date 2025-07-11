using System.Text;
using Microsoft.Extensions.Logging;
using _3dEYE.Sorter.Models;

namespace _3dEYE.Sorter;

public class StreamingSorter(
    ILogger logger,
    int maxMemoryLines = 100000, // Maximum lines to keep in memory
    int bufferSize = 1024 * 1024) // 1MB buffer for I/O
    : ISorter
{
    public async Task SortAsync(
        string inputFilePath, 
        string outputFilePath, 
        IComparer<LineData> comparer, 
        CancellationToken cancellationToken = default)
    {
        await SortAsync(inputFilePath, outputFilePath, bufferSize, comparer, cancellationToken).ConfigureAwait(false);
    }

    public async Task SortAsync(
        string inputFilePath, 
        string outputFilePath, 
        int bufferSizeBytes, 
        IComparer<LineData> comparer, 
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException($"Input file not found: {inputFilePath}");

        var fileInfo = new FileInfo(inputFilePath);
        logger.LogInformation("Starting streaming sort for file: {FilePath} ({Size} bytes) with max {MaxLines} lines in memory", 
            inputFilePath, fileInfo.Length, maxMemoryLines);

        var startTime = DateTime.UtcNow;

        try
        {
            await PerformStreamingSortAsync(
                inputFilePath, 
                outputFilePath, 
                bufferSizeBytes, 
                comparer, 
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during streaming sort");
            throw;
        }

        var totalTime = DateTime.UtcNow - startTime;
        var outputInfo = new FileInfo(outputFilePath);
        logger.LogInformation("Streaming sort completed in {TotalTime:g}. Output file: {OutputPath} ({Size} bytes)", 
            totalTime, outputFilePath, outputInfo.Length);
    }

    private async Task PerformStreamingSortAsync(
        string inputFilePath,
        string outputFilePath,
        int bufSize,
        IComparer<LineData> comparer,
        CancellationToken cancellationToken)
    {
        var fileInfo = new FileInfo(inputFilePath);
        if (fileInfo.Length == 0)
        {
            await File.WriteAllTextAsync(outputFilePath, "", cancellationToken).ConfigureAwait(false);
            return;
        }

        var tempFile = Path.GetTempFileName();
        
        try
        {
            await StreamAndSortAsync(inputFilePath, tempFile, bufSize, comparer, cancellationToken).ConfigureAwait(false);
            await WriteFinalResultAsync(tempFile, outputFilePath, bufSize, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    private async Task StreamAndSortAsync(
        string inputFilePath,
        string tempFilePath,
        int bufSize,
        IComparer<LineData> comparer,
        CancellationToken cancellationToken)
    {
        // Use a sorted set to maintain order with minimal memory
        var sortedBuffer = new SortedSet<LineData>(comparer);
        var linesProcessed = 0L;
        var batchesWritten = 0;
        
        using var reader = new StreamReader(inputFilePath, Encoding.UTF8, true, bufSize);
        await using var writer = new StreamWriter(tempFilePath, false, Encoding.UTF8, bufSize);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line == null) break;
            
            var lineData = LineData.FromString(line);

            // Add to sorted buffer
            sortedBuffer.Add(lineData);
            linesProcessed++;
            
            // If buffer is full, write the smallest elements to temp file
            if (sortedBuffer.Count >= maxMemoryLines)
            {
                await FlushBufferToTempAsync(sortedBuffer, writer, cancellationToken).ConfigureAwait(false);
                batchesWritten++;
                
                logger.LogDebug("Flushed batch {Batch} with {Lines} lines. Total processed: {TotalLines}", 
                    batchesWritten, maxMemoryLines, linesProcessed);
            }
        }
        
        // Write remaining elements in sorted order
        if (sortedBuffer.Count > 0)
        {
            await FlushBufferToTempAsync(sortedBuffer, writer, cancellationToken).ConfigureAwait(false);
            batchesWritten++;
            
            logger.LogDebug("Flushed final batch {Batch} with {Lines} lines. Total processed: {TotalLines}", 
                batchesWritten, sortedBuffer.Count, linesProcessed);
        }
        
        logger.LogInformation("Streaming phase completed. Processed {TotalLines} lines in {Batches} batches", 
            linesProcessed, batchesWritten);
    }

    private async Task FlushBufferToTempAsync(
        SortedSet<LineData> buffer,
        StreamWriter writer,
        CancellationToken cancellationToken)
    {
        foreach (var lineData in buffer)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(lineData.Content, cancellationToken).ConfigureAwait(false);
        }
        
        buffer.Clear();
    }

    private async Task WriteFinalResultAsync(
        string tempFilePath,
        string outputFilePath,
        int bufSize,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(tempFilePath, Encoding.UTF8, true, bufSize);
        await using var writer = new StreamWriter(outputFilePath, false, Encoding.UTF8, bufSize);
        
        var buffer = new char[bufSize];
        int bytesRead;
        
        while ((bytesRead = await reader.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
        }
    }
} 