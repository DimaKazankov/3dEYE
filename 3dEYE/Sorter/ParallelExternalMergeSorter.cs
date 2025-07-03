using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using _3dEYE.Sorter.Models;

namespace _3dEYE.Sorter;

public class ParallelExternalMergeSorter(
    ILogger logger,
    int defaultBufferSize = 1024 * 1024, // 1MB default
    string? tempDirectory = null,
    int maxDegreeOfParallelism = 0) // 0 means use Environment.ProcessorCount
    : ISorter
{
    private readonly string _tempDirectory = tempDirectory ?? Path.GetTempPath();
    private readonly int _maxDegreeOfParallelism = maxDegreeOfParallelism > 0 
        ? maxDegreeOfParallelism 
        : Environment.ProcessorCount;

    public async Task SortAsync(
        string inputFilePath, 
        string outputFilePath, 
        IComparer<LineData> comparer, 
        CancellationToken cancellationToken = default)
    {
        await SortAsync(inputFilePath, outputFilePath, defaultBufferSize, comparer, cancellationToken).ConfigureAwait(false);
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
        logger.LogInformation("Starting parallel external merge sort for file: {FilePath} ({Size} bytes) with {ThreadCount} threads", 
            inputFilePath, fileInfo.Length, _maxDegreeOfParallelism);

        var bufferSize = ValidateAndAdjustBufferSize(bufferSizeBytes, fileInfo.Length);
        
        var tempDir = Path.Combine(_tempDirectory, $"parallel_sort_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            await PerformParallelExternalMergeSortAsync(
                inputFilePath, 
                outputFilePath, 
                tempDir, 
                bufferSize, 
                comparer, 
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during parallel external merge sort");
            throw;
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    private async Task PerformParallelExternalMergeSortAsync(
        string inputFilePath,
        string outputFilePath,
        string tempDirectory,
        int bufferSize,
        IComparer<LineData> comparer,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Phase 1: Splitting file into chunks with buffer size {BufferSize} bytes using {ThreadCount} threads", 
            bufferSize, _maxDegreeOfParallelism);
        
        var chunkFiles = await SplitIntoChunksParallelAsync(
            inputFilePath, 
            tempDirectory, 
            bufferSize,
            comparer,
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Created {ChunkCount} sorted chunks using parallel processing", chunkFiles.Count);

        if (chunkFiles.Count == 0)
        {
            logger.LogWarning("No chunks created, creating empty output file");
            await File.WriteAllTextAsync(outputFilePath, "", cancellationToken).ConfigureAwait(false);
            return;
        }

        logger.LogInformation("Phase 2: Merging {ChunkCount} chunks", chunkFiles.Count);
        
        var estimatedPasses = EstimateMergePasses(chunkFiles.Count);
        logger.LogInformation("Estimated merge passes: {Passes}", estimatedPasses);

        await MergeChunksParallelAsync(chunkFiles, outputFilePath, bufferSize, comparer, cancellationToken).ConfigureAwait(false);

        var outputInfo = new FileInfo(outputFilePath);
        logger.LogInformation("Parallel sort completed. Output file: {OutputPath} ({Size} bytes)", 
            outputFilePath, outputInfo.Length);

        CleanupChunks(chunkFiles, logger);
    }

    private async Task<List<string>> SplitIntoChunksParallelAsync(
        string inputFilePath,
        string tempDirectory,
        int bufferSize,
        IComparer<LineData> comparer,
        CancellationToken cancellationToken)
    {
        var totalLines = await CountLinesAsync(inputFilePath, cancellationToken).ConfigureAwait(false);
        var optimalChunkSize = CalculateOptimalChunkSize(bufferSize, totalLines, _maxDegreeOfParallelism);
        
        logger.LogDebug("File has {TotalLines} lines, using chunk size of {ChunkSize} lines per thread", 
            totalLines, optimalChunkSize);

        var chunkFiles = new ConcurrentBag<string>();
        using var semaphore = new SemaphoreSlim(_maxDegreeOfParallelism, _maxDegreeOfParallelism);
        
        var tasks = new List<Task>();
        var chunkIndex = 0;
        
        using var reader = new StreamReader(inputFilePath, Encoding.UTF8, true, bufferSize);
        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            // Read a chunk of lines
            var lines = new List<LineData>();
            var linesRead = 0;

            while (linesRead < optimalChunkSize && !reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line == null) break;
                
                var lineData = LineData.FromString(line);
                lines.Add(lineData);
                linesRead++;
            }
            
            if (lines.Count == 0) break;
            
            var currentChunkIndex = chunkIndex++;
            var task = ProcessChunkParallelAsync(
                lines, 
                tempDirectory, 
                currentChunkIndex, 
                bufferSize, 
                comparer, 
                semaphore, 
                chunkFiles, 
                cancellationToken);
            
            tasks.Add(task);
        }
        
        await Task.WhenAll(tasks).ConfigureAwait(false);
        
        return chunkFiles.ToList();
    }

    private async Task ProcessChunkParallelAsync(
        List<LineData> lines,
        string tempDirectory,
        int chunkIndex,
        int bufferSize,
        IComparer<LineData> comparer,
        SemaphoreSlim semaphore,
        ConcurrentBag<string> chunkFiles,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        
        try
        {
            lines.Sort(comparer);
            
            var chunkFilePath = Path.Combine(tempDirectory, $"chunk_{chunkIndex:D6}.tmp");
            
            await using var writer = new StreamWriter(chunkFilePath, false, Encoding.UTF8, bufferSize);
            
            foreach (var lineData in lines)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await writer.WriteLineAsync(lineData.Content, cancellationToken).ConfigureAwait(false);
            }
            
            chunkFiles.Add(chunkFilePath);
            
            logger.LogDebug("Processed chunk {ChunkIndex} with {LineCount} lines", chunkIndex, lines.Count);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task MergeChunksParallelAsync(
        List<string> chunkFiles,
        string outputFilePath,
        int bufferSize,
        IComparer<LineData> comparer,
        CancellationToken cancellationToken)
    {
        if (chunkFiles.Count == 1)
        {
            File.Copy(chunkFiles[0], outputFilePath, true);
            return;
        }

        var currentChunks = chunkFiles;
        var passNumber = 0;
        
        while (currentChunks.Count > 1)
        {
            passNumber++;
            logger.LogInformation("Merge pass {PassNumber}: merging {ChunkCount} chunks", passNumber, currentChunks.Count);
            
            var mergedChunks = new ConcurrentBag<string>();
            var semaphore = new SemaphoreSlim(_maxDegreeOfParallelism, _maxDegreeOfParallelism);
            
            var mergeTasks = new List<Task>();
            
            for (var i = 0; i < currentChunks.Count; i += 2)
            {
                if (i + 1 < currentChunks.Count)
                {
                    var task = MergeTwoChunksAsync(
                        currentChunks[i], 
                        currentChunks[i + 1], 
                        outputFilePath, 
                        bufferSize, 
                        comparer, 
                        semaphore, 
                        mergedChunks, 
                        passNumber, 
                        i / 2, 
                        cancellationToken);
                    mergeTasks.Add(task);
                }
                else
                {
                    var task = CopySingleChunkAsync(
                        currentChunks[i], 
                        outputFilePath, 
                        semaphore, 
                        mergedChunks, 
                        passNumber, 
                        i / 2, 
                        cancellationToken);
                    mergeTasks.Add(task);
                }
            }
            
            await Task.WhenAll(mergeTasks).ConfigureAwait(false);
            semaphore.Dispose();
            
            CleanupChunks(currentChunks, logger);
            currentChunks = mergedChunks.ToList();
        }
        
        if (currentChunks.Count == 1)
        {
            File.Move(currentChunks[0], outputFilePath, true);
        }
    }

    private async Task MergeTwoChunksAsync(
        string chunk1Path,
        string chunk2Path,
        string outputBasePath,
        int bufferSize,
        IComparer<LineData> comparer,
        SemaphoreSlim semaphore,
        ConcurrentBag<string> mergedChunks,
        int passNumber,
        int mergeIndex,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        
        try
        {
            var mergedChunkPath = Path.Combine(
                Path.GetDirectoryName(outputBasePath)!, 
                $"merged_pass{passNumber:D3}_{mergeIndex:D6}.tmp");
            
            await using var writer = new StreamWriter(mergedChunkPath, false, Encoding.UTF8, bufferSize);
            using var reader1 = new StreamReader(chunk1Path, Encoding.UTF8, true, bufferSize);
            using var reader2 = new StreamReader(chunk2Path, Encoding.UTF8, true, bufferSize);
            
            var line1 = await reader1.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            var line2 = await reader2.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            
            while (line1 != null && line2 != null && !cancellationToken.IsCancellationRequested)
            {
                var lineData1 = LineData.FromString(line1);
                var lineData2 = LineData.FromString(line2);
                var comparison = comparer.Compare(lineData1, lineData2); 
                
                if (comparison <= 0)
                {
                    await writer.WriteLineAsync(line1.AsMemory(), cancellationToken).ConfigureAwait(false);
                    line1 = await reader1.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await writer.WriteLineAsync(line2.AsMemory(), cancellationToken).ConfigureAwait(false);
                    line2 = await reader2.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            
            while (line1 != null && !cancellationToken.IsCancellationRequested)
            {
                await writer.WriteLineAsync(line1.AsMemory(), cancellationToken).ConfigureAwait(false);
                line1 = await reader1.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            }
            
            while (line2 != null && !cancellationToken.IsCancellationRequested)
            {
                await writer.WriteLineAsync(line2.AsMemory(), cancellationToken).ConfigureAwait(false);
                line2 = await reader2.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            }
            
            mergedChunks.Add(mergedChunkPath);
            
            logger.LogDebug("Merged chunks {Chunk1} and {Chunk2} into {MergedChunk}", 
                Path.GetFileName(chunk1Path), Path.GetFileName(chunk2Path), Path.GetFileName(mergedChunkPath));
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task CopySingleChunkAsync(
        string chunkPath,
        string outputBasePath,
        SemaphoreSlim semaphore,
        ConcurrentBag<string> mergedChunks,
        int passNumber,
        int mergeIndex,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        
        try
        {
            var copiedChunkPath = Path.Combine(
                Path.GetDirectoryName(outputBasePath)!, 
                $"merged_pass{passNumber:D3}_{mergeIndex:D6}.tmp");
            
            File.Copy(chunkPath, copiedChunkPath, true);
            mergedChunks.Add(copiedChunkPath);
            
            logger.LogDebug("Copied single chunk {Chunk} to {CopiedChunk}", 
                Path.GetFileName(chunkPath), Path.GetFileName(copiedChunkPath));
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<long> CountLinesAsync(string filePath, CancellationToken cancellationToken)
    {
        var lineCount = 0L;
        using var reader = new StreamReader(filePath, Encoding.UTF8, true, 8192);
        
        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line == null) break;
            lineCount++;
        }
        
        return lineCount;
    }

    private int CalculateOptimalChunkSize(int bufferSize, long totalLines, int threadCount)
    {
        if (totalLines <= 10)
        {
            return Math.Max(1, (int)totalLines);
        }
        
        // Calculate optimal chunk size based on available memory and thread count
        // We want to ensure each thread has enough work but not too much to cause memory pressure
        var baseChunkSize = Math.Max(100, bufferSize / 200); // At least 100 lines per chunk (reduced from 1000)
        var optimalChunkSize = Math.Max(baseChunkSize, (int)(totalLines / (threadCount * 2))); // Ensure at least 2 chunks per thread
        
        // Cap the chunk size to prevent memory issues and ensure we don't exceed total lines
        var maxChunkSize = Math.Min(bufferSize / 100, (int)totalLines); // Conservative estimate, but don't exceed total lines
        return Math.Min(optimalChunkSize, maxChunkSize);
    }

    private int ValidateAndAdjustBufferSize(long requestedBufferSize, long fileSize)
    {
        var minBufferSize = 64 * 1024; // 64KB minimum
        var bufferSize = Math.Max((int)requestedBufferSize, minBufferSize);

        var maxBufferSize = Math.Max(fileSize / 10, minBufferSize); // At least minBufferSize
        bufferSize = Math.Min(bufferSize, (int)maxBufferSize);

        bufferSize = (int)Math.Pow(2, Math.Ceiling(Math.Log2(bufferSize)));
        bufferSize = Math.Max(bufferSize, minBufferSize);

        logger.LogDebug("Adjusted buffer size from {Requested} to {Adjusted} bytes", 
            requestedBufferSize, bufferSize);

        return bufferSize;
    }

    private static int EstimateMergePasses(int chunkCount)
    {
        return (int)Math.Ceiling(Math.Log2(chunkCount));
    }

    private static void CleanupChunks(IEnumerable<string> chunkFiles, ILogger logger)
    {
        foreach (var chunkFile in chunkFiles)
        {
            if (File.Exists(chunkFile))
                File.Delete(chunkFile);
        }
    }
} 