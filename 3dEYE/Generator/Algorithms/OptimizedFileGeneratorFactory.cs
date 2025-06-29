using Microsoft.Extensions.Logging;

namespace _3dEYE.Generator.Algorithms;

public class OptimizedFileGeneratorFactory
{
    private readonly ILogger _logger;
    private static readonly string[] SampleStrings = [
        "Apple", "Banana is yellow", "Cherry is the best", "Something something something",
        "Optimized for large files", "Batch processing enabled", "Memory efficient generation",
        "High performance I/O", "Reduced GC pressure", "Async operations"
    ];

    public OptimizedFileGeneratorFactory(ILogger logger)
    {
        _logger = logger;
    }

    public IFileGenerator GetOptimizedFileGenerator(int bufferSize = 1024 * 1024, int batchSize = 1000)
    {
        _logger.LogDebug("Creating OptimizedFileGenerator with buffer size: {BufferSize}, batch size: {BatchSize}", 
            bufferSize, batchSize);
        
        var generator = new OptimizedFileGenerator(_logger, SampleStrings, bufferSize, batchSize);
        _logger.LogInformation("OptimizedFileGenerator instance created successfully");
        
        return generator;
    }

    public IFileGenerator GetLargeFileGenerator()
    {
        // Optimized settings for files > 10GB
        const int largeBufferSize = 4 * 1024 * 1024; // 4MB buffer
        const int largeBatchSize = 2000; // Larger batches
        
        _logger.LogInformation("Creating large file optimized generator (buffer: {BufferSize}, batch: {BatchSize})", 
            largeBufferSize, largeBatchSize);
        
        return GetOptimizedFileGenerator(largeBufferSize, largeBatchSize);
    }

    public IFileGenerator GetUltraLargeFileGenerator()
    {
        // Optimized settings for files > 100GB
        const int ultraBufferSize = 8 * 1024 * 1024; // 8MB buffer
        const int ultraBatchSize = 5000; // Very large batches
        
        _logger.LogInformation("Creating ultra-large file optimized generator (buffer: {BufferSize}, batch: {BatchSize})", 
            ultraBufferSize, ultraBatchSize);
        
        return GetOptimizedFileGenerator(ultraBufferSize, ultraBatchSize);
    }
} 