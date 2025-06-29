using System.Text;
using Microsoft.Extensions.Logging;

namespace _3dEYE.Generator.Algorithms;

public class OptimizedFileGenerator : IFileGenerator
{
    private readonly ILogger _logger;
    private readonly string[] _input;
    private readonly int _bufferSize;
    private readonly int _batchSize;

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
        _input = input;
        _bufferSize = bufferSize;
        _batchSize = batchSize;
    }

    public async Task GenerateFileAsync(string filePath, long fileSizeInBytes)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty");
        if (fileSizeInBytes <= 0)
            throw new ArgumentException($"File size must be greater than 0, but was: {fileSizeInBytes}");

        PrepareDirectory(filePath);
        _logger.LogInformation("Starting optimized file generation. Target: {FilePath}, Size: {FileSize} bytes, Buffer: {BufferSize} bytes", 
            filePath, fileSizeInBytes, _bufferSize);

        try
        {
            var random = new Random();
            long currentSize = 0;
            var batchCount = 0;

            // Use FileStream with larger buffer for better I/O performance
            await using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, _bufferSize, FileOptions.Asynchronous);
            await using var writer = new StreamWriter(fileStream, Encoding.UTF8, _bufferSize, leaveOpen: false);

            _logger.LogInformation("File generation in progress with batch processing...");

            while (currentSize < fileSizeInBytes)
            {
                // Process in batches to reduce GC pressure
                var batch = GenerateBatch(random, fileSizeInBytes - currentSize);
                
                foreach (var line in batch)
                {
                    await writer.WriteLineAsync(line);
                    currentSize += Encoding.UTF8.GetByteCount(line + Environment.NewLine);
                    
                    if (currentSize >= fileSizeInBytes)
                        break;
                }

                batchCount++;

                // Force garbage collection periodically for very large files
                if (fileSizeInBytes > 10L * 1024L * 1024L * 1024L && batchCount % 1000 == 0) // 10GB+
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }

            await writer.FlushAsync();
            await fileStream.FlushAsync();

            _logger.LogInformation("Optimized file generation completed. Final size: {FinalSize} bytes, Batches processed: {BatchCount}", 
                currentSize, batchCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during optimized file generation: {FilePath}", filePath);
            throw;
        }
    }

    private IEnumerable<string> GenerateBatch(Random random, long remainingBytes)
    {
        var batch = new List<string>();
        var estimatedBytesPerLine = 50L; // Use long to avoid overflow
        
        // For very small remaining bytes, generate at least one line
        var maxLinesInBatch = Math.Max(1, Math.Min(_batchSize, (int)Math.Min(remainingBytes / estimatedBytesPerLine, int.MaxValue)));

        for (int i = 0; i < maxLinesInBatch; i++)
        {
            var number = random.Next(1, 1000000);
            var str = _input[random.Next(_input.Length)];
            var line = $"{number}. {str}";
            batch.Add(line);
        }

        return batch;
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