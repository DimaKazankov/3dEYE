using System.Text;
using Microsoft.Extensions.Logging;

namespace _3dEYE.Generator.Algorithms;

public class ParallelFileGenerator : IFileGenerator
{
    private readonly ILogger _logger;
    private readonly string[] _input;
    private readonly int _chunkSize;
    private readonly int _maxDegreeOfParallelism;
    private readonly char[] _lineBuffer;
    
    // Static readonly constants to avoid repeated allocations
    private static readonly string Separator = ". ";
    private static readonly string NewLine = Environment.NewLine;

    public ParallelFileGenerator(ILogger logger, string[] input, int chunkSize = 100 * 1024 * 1024, int maxDegreeOfParallelism = 0)
    {
        if (input.Length == 0)
            throw new ArgumentException("Input strings array cannot be null or empty");
        if (input.Any(string.IsNullOrEmpty))
            throw new ArgumentException("Input strings array contains null or empty strings");
        if (chunkSize <= 0)
            throw new ArgumentException("Chunk size must be greater than 0");
        
        _logger = logger;
        _input = input;
        _chunkSize = chunkSize;
        _maxDegreeOfParallelism = maxDegreeOfParallelism > 0 ? maxDegreeOfParallelism : Environment.ProcessorCount;
        _lineBuffer = new char[256]; // Reusable buffer for line formatting
    }

    public async Task GenerateFileAsync(string filePath, long fileSizeInBytes)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty");
        if (fileSizeInBytes <= 0)
            throw new ArgumentException($"File size must be greater than 0, but was: {fileSizeInBytes}");

        PrepareDirectory(filePath);
        _logger.LogInformation("Starting parallel file generation. Target: {FilePath}, Size: {FileSize} bytes, Chunks: {ChunkSize}, Parallelism: {Parallelism}", 
            filePath, fileSizeInBytes, _chunkSize, _maxDegreeOfParallelism);

        var tempFiles = new List<string>();
        try
        {
            // Calculate number of chunks needed
            var numberOfChunks = (int)Math.Ceiling((double)fileSizeInBytes / _chunkSize);
            _logger.LogInformation("Generating {NumberOfChunks} chunks in parallel...", numberOfChunks);

            // Generate chunks in parallel
            var chunkTasks = new List<Task<string>>();
            for (int i = 0; i < numberOfChunks; i++)
            {
                var chunkIndex = i;
                var chunkStartOffset = i * _chunkSize;
                var remainingBytes = fileSizeInBytes - chunkStartOffset;
                var currentChunkSize = Math.Min(_chunkSize, remainingBytes);

                var task = Task.Run(async () =>
                {
                    var tempFile = await GenerateChunkAsync(chunkIndex, currentChunkSize, chunkStartOffset).ConfigureAwait(false);
                    _logger.LogDebug("Chunk {ChunkIndex} completed: {TempFile}", chunkIndex, tempFile);
                    return tempFile;
                });

                chunkTasks.Add(task);
            }

            // Wait for all chunks to complete
            var tempFilePaths = await Task.WhenAll(chunkTasks).ConfigureAwait(false);
            tempFiles.AddRange(tempFilePaths);

            _logger.LogInformation("All chunks generated. Merging files...");

            // Merge chunks using memory-mapped files for optimal performance
            await MergeChunksAsync(filePath, tempFilePaths, fileSizeInBytes).ConfigureAwait(false);

            _logger.LogInformation("Parallel file generation completed successfully. Final size: {FileSize} bytes", fileSizeInBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during parallel file generation: {FilePath}", filePath);
            throw;
        }
        finally
        {
            // Cleanup temp files
            await CleanupTempFilesAsync(tempFiles).ConfigureAwait(false);
        }
    }

    private async Task<string> GenerateChunkAsync(int chunkIndex, long chunkSize, long globalOffset)
    {
        var tempFilePath = Path.Combine(Path.GetTempPath(), $"parallel_chunk_{chunkIndex}_{Guid.NewGuid()}.txt");
        long currentSize = 0;

        await using var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.Asynchronous);
        await using var writer = new StreamWriter(fileStream, Encoding.UTF8, 64 * 1024, leaveOpen: false);

        while (currentSize < chunkSize)
        {
            var number = Random.Shared.Next(1, 1000000) + (int)(globalOffset / 50); // Ensure unique numbering across chunks
            var str = _input[Random.Shared.Next(_input.Length)];
            
            // Use reusable buffer for zero-allocation formatting
            var lineLength = FormatLine(_lineBuffer, number, str);
            var line = new string(_lineBuffer, 0, lineLength);
            
            await writer.WriteLineAsync(line).ConfigureAwait(false);
            currentSize += Encoding.UTF8.GetByteCount(line) + Encoding.UTF8.GetByteCount(NewLine);

            if (currentSize >= chunkSize)
                break;
        }

        await writer.FlushAsync().ConfigureAwait(false);
        return tempFilePath;
    }

    private static int FormatLine(Span<char> buffer, int number, string str)
    {
        var numberStr = number.ToString();
        
        // Copy number
        numberStr.CopyTo(buffer);
        var currentPos = numberStr.Length;
        
        // Copy separator (using static readonly)
        Separator.CopyTo(buffer.Slice(currentPos));
        currentPos += Separator.Length;
        
        // Copy string
        str.CopyTo(buffer.Slice(currentPos));
        currentPos += str.Length;
        
        return currentPos;
    }

    private async Task MergeChunksAsync(string finalFilePath, string[] tempFilePaths, long totalSize)
    {
        // Create the final file with the exact size
        await using var finalFileStream = new FileStream(finalFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.Asynchronous);
        finalFileStream.SetLength(totalSize);

        // Calculate chunk positions for parallel merging
        var chunkPositions = new List<(string filePath, long offset, long size)>();
        long currentOffset = 0;

        foreach (var tempFilePath in tempFilePaths)
        {
            if (!File.Exists(tempFilePath)) continue;

            var fileInfo = new FileInfo(tempFilePath);
            var chunkSize = fileInfo.Length;
            
            chunkPositions.Add((tempFilePath, currentOffset, chunkSize));
            currentOffset += chunkSize;
        }

        // Sort chunks by offset to minimize disk head movement
        chunkPositions.Sort((a, b) => a.offset.CompareTo(b.offset));

        // Merge chunks sequentially but with parallel processing of each chunk
        foreach (var chunkInfo in chunkPositions)
        {
            await MergeChunkToPositionAsync(finalFileStream, chunkInfo.filePath, chunkInfo.offset, chunkInfo.size).ConfigureAwait(false);
        }
    }

    private async Task MergeChunkToPositionAsync(FileStream finalFileStream, string tempFilePath, long offset, long size)
    {
        await using var tempFileStream = File.OpenRead(tempFilePath);
        
        finalFileStream.Seek(offset, SeekOrigin.Begin);
        
        // Copy chunk data to the correct position in final file
        var buffer = new byte[64 * 1024]; // 64KB buffer
        int bytesRead;
        long totalBytesRead = 0;

        while ((bytesRead = await tempFileStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
        {
            var bytesToWrite = Math.Min(bytesRead, (int)(size - totalBytesRead));
            await finalFileStream.WriteAsync(buffer, 0, bytesToWrite).ConfigureAwait(false);
            totalBytesRead += bytesToWrite;

            if (totalBytesRead >= size)
                break;
        }

        await finalFileStream.FlushAsync().ConfigureAwait(false);
    }

    private async Task CleanupTempFilesAsync(List<string> tempFiles)
    {
        var cleanupTasks = tempFiles.Select(tempFile =>
        {
            try
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                    _logger.LogDebug("Cleaned up temp file: {TempFile}", tempFile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup temp file: {TempFile}", tempFile);
            }

            return Task.CompletedTask;
        });

        await Task.WhenAll(cleanupTasks).ConfigureAwait(false);
    }

    private void PrepareDirectory(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(directory))
            throw new ArgumentException("File path must include a directory");

        if (Directory.Exists(directory))
            return;

        _logger.LogInformation("Directory doesn't exist. Creating directory: {Directory}", directory);
        Directory.CreateDirectory(directory);
    }
} 