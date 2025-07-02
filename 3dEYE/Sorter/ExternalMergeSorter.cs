using Microsoft.Extensions.Logging;
using _3dEYE.Sorter.Models;
using _3dEYE.Sorter.Utils;

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
        logger.LogInformation("Starting external merge sort for file: {FilePath} ({Size} bytes)", 
            inputFilePath, fileInfo.Length);

        // Validate available disk space for temporary files
        ValidateDiskSpace(fileInfo.Length, _tempDirectory);

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
            logger.LogError(ex, "Error during external merge sort");
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
                logger.LogWarning("Failed to clean up temporary directory {TempDir}: {Message}", tempDir, cleanupEx.Message);
            }
        }
    }

    private async Task PerformExternalMergeSortAsync(
        string inputFilePath,
        string outputFilePath,
        string tempDirectory,
        int bufferSize,
        IComparer<LineData> comparer,
        CancellationToken cancellationToken)
    {
        var chunkManager = new ChunkManager(bufferSize);
        var mergeManager = new MergeManager(bufferSize, comparer);

        // Phase 1: Split file into sorted chunks
        logger.LogInformation("Phase 1: Splitting file into chunks with buffer size {BufferSize} bytes", bufferSize);
        
        var startTime = DateTime.UtcNow;
        var chunkFiles = await chunkManager.SplitIntoChunksAsync(
            inputFilePath, 
            tempDirectory, 
            comparer, // Pass the comparer to ChunkManager
            cancellationToken).ConfigureAwait(false);

        var splitTime = DateTime.UtcNow - startTime;
        logger.LogInformation("Created {ChunkCount} sorted chunks in {SplitTime:g}", chunkFiles.Count, splitTime);

        if (chunkFiles.Count == 0)
        {
            logger.LogWarning("No chunks created, creating empty output file");
            await File.WriteAllTextAsync(outputFilePath, "", cancellationToken).ConfigureAwait(false);
            return;
        }

        // Phase 2: Merge chunks into final sorted file
        logger.LogInformation("Phase 2: Merging {ChunkCount} chunks", chunkFiles.Count);
        
        var estimatedPasses = MergeManager.EstimateMergePasses(chunkFiles.Count);
        logger.LogInformation("Estimated merge passes: {Passes}", estimatedPasses);

        var mergeStartTime = DateTime.UtcNow;
        await mergeManager.MergeChunksAsync(chunkFiles, outputFilePath, cancellationToken).ConfigureAwait(false);
        var mergeTime = DateTime.UtcNow - mergeStartTime;

        // Verify the output
        var outputInfo = new FileInfo(outputFilePath);
        var totalTime = DateTime.UtcNow - startTime;
        logger.LogInformation("Sort completed in {TotalTime:g}. Merge  time {mergeTime:g}. Output file: {OutputPath} ({Size} bytes)", 
            totalTime, mergeTime, outputFilePath, outputInfo.Length);

        // Clean up chunk files
        ChunkManager.CleanupChunks(chunkFiles, logger);
    }

    private int ValidateAndAdjustBufferSize(long requestedBufferSize, long fileSize)
    {
        // Ensure minimum buffer size
        var minBufferSize = 64 * 1024; // 64KB minimum
        var bufferSize = Math.Max((int)requestedBufferSize, minBufferSize);

        // For very large files (100GB+), use more conservative buffer sizing
        // Don't use more than 2% of file size or 25MB for very large files
        var maxBufferSize = fileSize > 100L * 1024 * 1024 * 1024 // 100GB
            ? Math.Min(fileSize / 50, 25 * 1024 * 1024) // 2% of file size or 25MB
            : fileSize > 50L * 1024 * 1024 * 1024 // 50GB
            ? Math.Min(fileSize / 20, 50 * 1024 * 1024) // 5% of file size or 50MB
            : Math.Max(fileSize / 10, minBufferSize); // 10% of file size, but at least minBufferSize
        
        bufferSize = Math.Min(bufferSize, (int)maxBufferSize);

        // Round to nearest power of 2 for better performance
        bufferSize = (int)Math.Pow(2, Math.Ceiling(Math.Log2(bufferSize)));

        // Final safety check: ensure buffer size is never 0
        bufferSize = Math.Max(bufferSize, minBufferSize);

        logger.LogDebug("Adjusted buffer size from {Requested} to {Adjusted} bytes for file size {FileSize}", 
            requestedBufferSize, bufferSize, fileSize);

        return bufferSize;
    }

    private void ValidateDiskSpace(long fileSize, string tempDirectory)
    {
        try
        {
            var driveInfo = new DriveInfo(Path.GetPathRoot(tempDirectory) ?? tempDirectory);
            var requiredSpace = fileSize * 3; // Estimate: input + output + temp files
            var availableSpace = driveInfo.AvailableFreeSpace;
            
            if (availableSpace < requiredSpace)
            {
                logger.LogWarning("Available disk space ({Available} bytes) may be insufficient for file size {FileSize} bytes. Required: ~{Required} bytes", 
                    availableSpace, fileSize, requiredSpace);
            }
            else
            {
                logger.LogDebug("Disk space validation passed. Available: {Available} bytes, Required: ~{Required} bytes", 
                    availableSpace, requiredSpace);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning("Could not validate disk space: {Message}", ex.Message);
        }
    }
}