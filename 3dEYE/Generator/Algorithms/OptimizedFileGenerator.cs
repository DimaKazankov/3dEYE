using System.Text;
using Microsoft.Extensions.Logging;
using _3dEYE.Helpers;

namespace _3dEYE.Generator.Algorithms;

public class OptimizedFileGenerator : IFileGenerator
{
    private readonly ILogger _logger;
    private readonly ReadOnlyMemory<char>[] _inputMemory;
    private readonly int _bufferSize;
    private readonly int _batchSize;
    private readonly char[] _lineBuffer;

    public OptimizedFileGenerator(ILogger logger, string[] input, int bufferSize = 1024 * 1024, int batchSize = 1000)
    {
        if (input.Length == 0)
            throw new ArgumentException("Input strings array cannot be null or empty");
        if (input.Any(string.IsNullOrEmpty))
            throw new ArgumentException("Input strings array contains null or empty strings");
        if (bufferSize <= 0)
            throw new ArgumentException("Buffer size must be greater than 0");
        if (batchSize <= 0)
            throw new ArgumentException("Batch size must be greater than 0");
        
        _logger = logger;
        _inputMemory = FileGeneratorHelpers.ConvertToReadOnlyMemoryArray(input);
        _bufferSize = bufferSize;
        _batchSize = batchSize;
        _lineBuffer = new char[512];
    }

    public async Task GenerateFileAsync(string filePath, long fileSizeInBytes)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty");
        if (fileSizeInBytes <= 0)
            throw new ArgumentException($"File size must be greater than 0, but was: {fileSizeInBytes}");

        FileGeneratorHelpers.PrepareDirectory(filePath, _logger);
        _logger.LogInformation("Starting optimized file generation. Target: {FilePath}, Size: {FileSize} bytes, Buffer: {BufferSize} bytes", 
            filePath, fileSizeInBytes, _bufferSize);

        try
        {
            long currentSize = 0;
            var batchCount = 0;

            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, _bufferSize, FileOptions.Asynchronous);
            await using var writer = new StreamWriter(fileStream, Encoding.UTF8, _bufferSize, leaveOpen: false);

            _logger.LogInformation("File generation in progress with batch processing...");

            while (currentSize < fileSizeInBytes)
            {
                var batch = GenerateBatch(fileSizeInBytes - currentSize);
                
                foreach (var line in batch)
                {
                    await writer.WriteLineAsync(line).ConfigureAwait(false);
                    currentSize += FileGeneratorHelpers.CalculateLineByteCount(line);
                    
                    if (currentSize >= fileSizeInBytes)
                        break;
                }

                batchCount++;

                if (fileSizeInBytes > 10L * 1024L * 1024L * 1024L && batchCount % 1000 == 0) // 10GB+
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }

            await writer.FlushAsync().ConfigureAwait(false);
            await fileStream.FlushAsync().ConfigureAwait(false);

            _logger.LogInformation("Optimized file generation completed. Final size: {FinalSize} bytes, Batches processed: {BatchCount}", 
                currentSize, batchCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during optimized file generation: {FilePath}", filePath);
            throw;
        }
    }

    private IEnumerable<ReadOnlyMemory<char>> GenerateBatch(long remainingBytes)
    {
        var batch = new List<ReadOnlyMemory<char>>();
        const long estimatedBytesPerLine = 50L; 
        
        var maxLinesInBatch = Math.Max(1, Math.Min(_batchSize, (int)Math.Min(remainingBytes / estimatedBytesPerLine, int.MaxValue)));

        for (var i = 0; i < maxLinesInBatch; i++)
        {
            var lineMemory = FileGeneratorHelpers.GenerateRandomLineAsMemory(_inputMemory, _lineBuffer);
            batch.Add(lineMemory);
        }

        return batch;
    }
} 