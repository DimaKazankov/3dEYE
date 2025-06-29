using Microsoft.Extensions.Logging;

namespace _3dEYE.Generator.Algorithms;

public class ParallelFileGeneratorFactory
{
    private readonly ILogger _logger;
    private static readonly string[] SampleStrings = [
        "Apple", "Banana is yellow", "Cherry is the best", "Something something something",
        "Parallel processing enabled", "Memory-mapped file merging", "High performance generation",
        "Multi-threaded file creation", "Optimized for large files", "Chunk-based processing"
    ];

    public ParallelFileGeneratorFactory(ILogger logger)
    {
        _logger = logger;
    }

    public IFileGenerator GetParallelFileGenerator(int chunkSize = 100 * 1024 * 1024, int maxDegreeOfParallelism = 0)
    {
        _logger.LogDebug("Creating ParallelFileGenerator with chunk size: {ChunkSize}, parallelism: {Parallelism}", 
            chunkSize, maxDegreeOfParallelism);
        
        var generator = new ParallelFileGenerator(_logger, SampleStrings, chunkSize, maxDegreeOfParallelism);
        _logger.LogInformation("ParallelFileGenerator instance created successfully");
        
        return generator;
    }

    public IFileGenerator GetLargeFileParallelGenerator()
    {
        // Optimized settings for files > 1GB
        const int largeChunkSize = 200 * 1024 * 1024; // 200MB chunks
        const int largeParallelism = 4; // 4 parallel threads
        
        _logger.LogInformation("Creating large file parallel generator (chunk: {ChunkSize}, parallelism: {Parallelism})", 
            largeChunkSize, largeParallelism);
        
        return GetParallelFileGenerator(largeChunkSize, largeParallelism);
    }

    public IFileGenerator GetUltraLargeFileParallelGenerator()
    {
        // Optimized settings for files > 10GB
        const int ultraChunkSize = 500 * 1024 * 1024; // 500MB chunks
        const int ultraParallelism = 8; // 8 parallel threads
        
        _logger.LogInformation("Creating ultra-large file parallel generator (chunk: {ChunkSize}, parallelism: {Parallelism})", 
            ultraChunkSize, ultraParallelism);
        
        return GetParallelFileGenerator(ultraChunkSize, ultraParallelism);
    }

    public IFileGenerator GetBalancedParallelGenerator()
    {
        // Balanced settings for medium files (100MB - 1GB)
        const int balancedChunkSize = 50 * 1024 * 1024; // 50MB chunks
        const int balancedParallelism = 2; // 2 parallel threads
        
        _logger.LogInformation("Creating balanced parallel generator (chunk: {ChunkSize}, parallelism: {Parallelism})", 
            balancedChunkSize, balancedParallelism);
        
        return GetParallelFileGenerator(balancedChunkSize, balancedParallelism);
    }
} 