using System.Buffers;
using Microsoft.Extensions.Logging;
using _3dEYE.Helpers;
using _3dEYE.Sorter.Models;
using _3dEYE.Sorter.Utils;

namespace _3dEYE.Generator.Algorithms;

public class ThreeDEyeFilesGenerator : IFileGenerator
{
    private readonly ILogger _logger;
    private readonly ReadOnlyMemory<char>[] _inputMemory;
    private readonly int _chunkSize;
    private readonly int _maxDegreeOfParallelism;

    public ThreeDEyeFilesGenerator(ILogger logger, string[] input, 
        int chunkSize = 100 * 1024 * 1024, int maxDegreeOfParallelism = 0)
    {
        if (input.Length == 0)
            throw new ArgumentException("Input strings array cannot be null or empty");
        if (input.Any(string.IsNullOrEmpty))
            throw new ArgumentException("Input strings array contains null or empty strings");
        if (chunkSize <= 0)
            throw new ArgumentException("Chunk size must be greater than 0");
        
        _logger = logger;
        _inputMemory = FileGeneratorHelpers.ConvertToReadOnlyMemoryArray(input);
        _chunkSize = chunkSize;
        _maxDegreeOfParallelism = maxDegreeOfParallelism > 0 ? maxDegreeOfParallelism : Environment.ProcessorCount;
    }

    public async Task GenerateFileAsync(string filePath, long fileSizeInBytes)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty");
        if (fileSizeInBytes <= 0)
            throw new ArgumentException($"File size must be greater than 0, but was: {fileSizeInBytes}");

        FileGeneratorHelpers.PrepareDirectory(filePath, _logger);
        _logger.LogInformation("Starting parallel file generation. Target: {FilePath}, Size: {FileSize} bytes, Chunks: {ChunkSize}, Parallelism: {Parallelism}", 
            filePath, fileSizeInBytes, _chunkSize, _maxDegreeOfParallelism);

        var tempFiles = new List<string>();
        try
        {
            var tempDir = Path.GetTempPath();
            var numberOfChunks = (int)Math.Ceiling((double)fileSizeInBytes / _chunkSize);
            _logger.LogInformation("Generating {NumberOfChunks} chunks in parallel using FileChunkProcessor approach...", numberOfChunks);

            var chunkTasks = new List<Task<IReadOnlyList<string>>>();
            for (var i = 0; i < numberOfChunks; i++)
            {
                var chunkIndex = i;
                var chunkStartOffset = i * _chunkSize;
                var remainingBytes = fileSizeInBytes - chunkStartOffset;
                var currentChunkSize = Math.Min(_chunkSize, remainingBytes);

                var task = Task.Run(async () =>
                {
                    var chunkPaths = await GenerateChunkWithFileChunkProcessorApproachAsync(
                        tempDir, chunkIndex, currentChunkSize, chunkStartOffset).ConfigureAwait(false);
                    _logger.LogDebug("Chunk {ChunkIndex} completed with {ChunkCount} sub-chunks", chunkIndex, chunkPaths.Count);
                    return chunkPaths;
                });

                chunkTasks.Add(task);
            }

            var allChunkResults = await Task.WhenAll(chunkTasks).ConfigureAwait(false);
            foreach (var chunkPaths in allChunkResults)
            {
                tempFiles.AddRange(chunkPaths);
            }

            _logger.LogInformation("All chunks generated. Merging files...");

            await MergeChunksAsync(filePath, tempFiles.ToArray()).ConfigureAwait(false);
            _logger.LogInformation("Parallel file generation completed successfully. Final size: {FileSize} bytes", fileSizeInBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during parallel file generation: {FilePath}", filePath);
            throw;
        }
        finally
        {
            CleanupTempFilesAsync(tempFiles);
        }
    }

    private async Task<IReadOnlyList<string>> GenerateChunkWithFileChunkProcessorApproachAsync(
        string tempDir,
        int chunkIndex,
        long chunkSize,
        long globalOffset,
        int maxLineChars = 1024)
    {
        if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

        _logger.LogDebug("GenerateChunkWithFileChunkProcessorApproachAsync: Starting chunk {ChunkIndex}, size {ChunkSize}, offset {GlobalOffset}", 
            chunkIndex, chunkSize, globalOffset);

        // Create single chunk file path
        var chunkPath = Path.Combine(tempDir, $"chunk_{chunkIndex:D4}_{Guid.NewGuid():N}.txt");
        var chunks = new List<string> { chunkPath };
        
        // Use a larger buffer to handle the entire chunk
        var bufferSize = Math.Min((int)chunkSize + maxLineChars, 64 * 1024 * 1024); // Cap at 64MB
        var buffer = ArrayPool<char>.Shared.Rent(bufferSize);
        var totalBytesGenerated = 0L;
        var lineNumber = (int)(globalOffset / 50) + 1; // Estimate line number based on offset

        _logger.LogDebug("GenerateChunkWithFileChunkProcessorApproachAsync: Initial lineNumber = {LineNumber}, bufferSize = {BufferSize}", lineNumber, bufferSize);

        try
        {
            // Generate the entire chunk in one go
            var entries = new List<LineEntry>((int)(chunkSize / 32)); // Estimate capacity
            var bufferPos = 0;
            var linesGenerated = 0;

            while (totalBytesGenerated < chunkSize && bufferPos < buffer.Length - maxLineChars)
            {
                // Generate a line in the format: <Number>. <String>
                var randomInput = _inputMemory[Random.Shared.Next(_inputMemory.Length)];
                
                // Calculate total line length first
                var numberLength = WriteNumberToBuffer(buffer, bufferPos, lineNumber);
                var totalLineLength = numberLength + 2 + randomInput.Length + 1; // ". " + "\n"
                
                // Check if we can fit the complete line
                if (bufferPos + totalLineLength > buffer.Length)
                {
                    _logger.LogDebug("GenerateChunkWithFileChunkProcessorApproachAsync: Buffer full, breaking");
                    break;
                }
                
                // Check if we can fit the complete line in the target size
                if (totalBytesGenerated + totalLineLength > chunkSize)
                {
                    _logger.LogDebug("GenerateChunkWithFileChunkProcessorApproachAsync: Target size reached, breaking");
                    break;
                }

                // Write number (already written above)
                bufferPos += numberLength;
                
                // Write ". "
                buffer[bufferPos++] = '.';
                buffer[bufferPos++] = ' ';
                
                // Write the random input string
                randomInput.Span.CopyTo(buffer.AsSpan(bufferPos));
                bufferPos += randomInput.Length;
                
                // Write newline
                buffer[bufferPos++] = '\n';

                // Create LineEntry with proper memory views - use the actual written data
                var fullLineStart = bufferPos - totalLineLength;
                var fullLineMemory = buffer.AsMemory(fullLineStart, totalLineLength);
                var keyStart = fullLineStart + numberLength + 2; // +2 for ". "
                var keyMemory = buffer.AsMemory(keyStart, randomInput.Length);
                
                entries.Add(new LineEntry(fullLineMemory, keyMemory, lineNumber));
                
                totalBytesGenerated += totalLineLength;
                lineNumber++;
                linesGenerated++;
            }

            _logger.LogDebug("GenerateChunkWithFileChunkProcessorApproachAsync: Generation completed - linesGenerated = {LinesGenerated}, totalBytesGenerated = {TotalBytes}", 
                linesGenerated, totalBytesGenerated);

            // Write the entire chunk to file using LineEntry
            if (entries.Count > 0)
            {
                await entries.FlushRunAsync(chunkPath);
                _logger.LogDebug("GenerateChunkWithFileChunkProcessorApproachAsync: Chunk written - path = {ChunkPath}, totalBytesGenerated = {TotalBytes}", 
                    chunkPath, totalBytesGenerated);
            }
            else
            {
                _logger.LogDebug("GenerateChunkWithFileChunkProcessorApproachAsync: No entries to write");
            }
        }
        finally
        {
            ArrayPool<char>.Shared.Return(buffer);
        }

        _logger.LogDebug("GenerateChunkWithFileChunkProcessorApproachAsync: Completed chunk {ChunkIndex} - totalBytesGenerated = {TotalBytes}", 
            chunkIndex, totalBytesGenerated);

        return chunks;
    }

    private static int WriteNumberToBuffer(char[] buffer, int startPos, int number)
    {
        // Convert number to chars without string allocation
        var pos = startPos;
        var temp = number;
        
        // Handle zero case
        if (temp == 0)
        {
            buffer[pos++] = '0';
            return 1;
        }
        
        // Count digits first
        var digitCount = 0;
        var temp2 = temp;
        while (temp2 > 0)
        {
            digitCount++;
            temp2 /= 10;
        }
        
        // Write digits in reverse order
        var endPos = pos + digitCount - 1;
        while (temp > 0)
        {
            buffer[endPos--] = (char)('0' + (temp % 10));
            temp /= 10;
        }
        
        return digitCount;
    }

    private async Task MergeChunksAsync(string finalFilePath, string[] tempFilePaths)
    {
        await using var finalFileStream = new FileStream(finalFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, FileOptions.Asynchronous);
        // Don't pre-allocate the file size - let it grow naturally
        // finalFileStream.SetLength(totalSize);

        // Simply concatenate all files in order
        foreach (var tempFilePath in tempFilePaths)
        {
            if (!File.Exists(tempFilePath)) continue;

            await using var tempFileStream = File.OpenRead(tempFilePath);
            await tempFileStream.CopyToAsync(finalFileStream).ConfigureAwait(false);
        }
    }



    private void CleanupTempFilesAsync(List<string> tempFiles)
    {
        tempFiles.ForEach(tempFile =>
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
        });
    }
} 