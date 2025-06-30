using Microsoft.Extensions.Logging;
using _3dEYE.Helpers;

namespace _3dEYE.Sorter;

public class ExternalMergeSorter(
    ILogger logger,
    int defaultBufferSize = 1024 * 1024, // 1MB default
    string? tempDirectory = null)
    : ISorter
{
    private readonly string _tempDirectory = tempDirectory ?? Path.GetTempPath();

    public async Task SortAsync(
        string inputFilePath, 
        string outputFilePath, 
        IComparer<string>? comparer = null, 
        CancellationToken cancellationToken = default)
    {
        await SortAsync(inputFilePath, outputFilePath, defaultBufferSize, comparer, cancellationToken).ConfigureAwait(false);
    }

    public async Task SortAsync(
        string inputFilePath, 
        string outputFilePath, 
        long bufferSizeBytes, 
        IComparer<string>? comparer = null, 
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException($"Input file not found: {inputFilePath}");

        var fileInfo = new FileInfo(inputFilePath);
        logger?.LogInformation("Starting external merge sort for file: {FilePath} ({Size} bytes)", 
            inputFilePath, fileInfo.Length);

        // Validate buffer size
        var bufferSize = ValidateAndAdjustBufferSize(bufferSizeBytes, fileInfo.Length);
        
        // Create temporary directory for chunks
        var tempDir = Path.Combine(_tempDirectory, $"sort_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            await PerformExternalMergeSortAsync(
                inputFilePath, 
                outputFilePath, 
                tempDir, 
                bufferSize, 
                comparer, 
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during external merge sort");
            throw;
        }
        finally
        {
            // Clean up temporary directory
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch (Exception cleanupEx)
            {
                logger?.LogWarning("Failed to clean up temporary directory {TempDir}: {Message}", tempDir, cleanupEx.Message);
            }
        }
    }

    private async Task PerformExternalMergeSortAsync(
        string inputFilePath,
        string outputFilePath,
        string tempDirectory,
        int bufferSize,
        IComparer<string>? comparer,
        CancellationToken cancellationToken)
    {
        var chunkManager = new ChunkManager(bufferSize);
        var mergeManager = new MergeManager(bufferSize, comparer);

        // Phase 1: Split file into sorted chunks
        logger?.LogInformation("Phase 1: Splitting file into chunks with buffer size {BufferSize} bytes", bufferSize);
        
        var chunkFiles = await chunkManager.SplitIntoChunksAsync(
            inputFilePath, 
            tempDirectory, 
            cancellationToken).ConfigureAwait(false);

        logger?.LogInformation("Created {ChunkCount} sorted chunks", chunkFiles.Count);

        if (chunkFiles.Count == 0)
        {
            logger?.LogWarning("No chunks created, creating empty output file");
            File.WriteAllText(outputFilePath, "");
            return;
        }

        // Phase 2: Merge chunks into final sorted file
        logger?.LogInformation("Phase 2: Merging {ChunkCount} chunks", chunkFiles.Count);
        
        var estimatedPasses = MergeManager.EstimateMergePasses(chunkFiles.Count);
        logger?.LogInformation("Estimated merge passes: {Passes}", estimatedPasses);

        await mergeManager.MergeChunksAsync(chunkFiles, outputFilePath, cancellationToken).ConfigureAwait(false);

        // Verify the output
        var outputInfo = new FileInfo(outputFilePath);
        logger?.LogInformation("Sort completed. Output file: {OutputPath} ({Size} bytes)", 
            outputFilePath, outputInfo.Length);

        // Clean up chunk files
        ChunkManager.CleanupChunks(chunkFiles, logger);
    }

    private int ValidateAndAdjustBufferSize(long requestedBufferSize, long fileSize)
    {
        // Ensure minimum buffer size
        var minBufferSize = 64 * 1024; // 64KB minimum
        var bufferSize = Math.Max((int)requestedBufferSize, minBufferSize);

        // Ensure maximum buffer size (don't use more than 10% of file size or 100MB)
        var maxBufferSize = Math.Min(fileSize / 10, 100 * 1024 * 1024);
        bufferSize = Math.Min(bufferSize, (int)maxBufferSize);

        // Round to nearest power of 2 for better performance
        bufferSize = (int)Math.Pow(2, Math.Ceiling(Math.Log2(bufferSize)));

        logger?.LogDebug("Adjusted buffer size from {Requested} to {Adjusted} bytes", 
            requestedBufferSize, bufferSize);

        return bufferSize;
    }

    public Task<SortStatistics> GetSortStatisticsAsync(
        string inputFilePath, 
        long bufferSizeBytes)
    {
        var fileInfo = new FileInfo(inputFilePath);
        var bufferSize = ValidateAndAdjustBufferSize(bufferSizeBytes, fileInfo.Length);
        
        var chunkManager = new ChunkManager(bufferSize);
        var estimatedChunks = chunkManager.EstimateChunkCount(fileInfo.Length);
        var estimatedPasses = MergeManager.EstimateMergePasses(estimatedChunks);

        var statistics = new SortStatistics
        {
            FileSizeBytes = fileInfo.Length,
            BufferSizeBytes = bufferSize,
            EstimatedChunks = estimatedChunks,
            EstimatedMergePasses = estimatedPasses,
            EstimatedTotalIOPerFile = estimatedPasses * 2 + 1 // Read + Write per pass + initial read
        };

        return Task.FromResult(statistics);
    }
}

public class SortStatistics
{
    public long FileSizeBytes { get; set; }
    public int BufferSizeBytes { get; set; }
    public int EstimatedChunks { get; set; }
    public int EstimatedMergePasses { get; set; }
    public int EstimatedTotalIOPerFile { get; set; }
    
    public string FileSizeFormatted => FormatBytes(FileSizeBytes);
    public string BufferSizeFormatted => FormatBytes(BufferSizeBytes);
    
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
} 