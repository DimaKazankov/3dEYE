using Microsoft.Extensions.Logging;

namespace _3dEYE.Generator.Algorithms;

public class FileGeneratorFactory(ILogger logger)
{
    private static readonly string[] SampleStrings = [
        "Apple", "Banana is yellow", "Cherry is the best", "Something something something",
        "Optimized for large files", "Batch processing enabled", "Memory efficient generation",
        "High performance I/O", "Reduced GC pressure", "Async operations",
        "Parallel processing enabled", "Memory-mapped file merging", "High performance generation",
        "Multi-threaded file creation", "Optimized for large files", "Chunk-based processing"
    ];

    public IFileGenerator CreateBasicGenerator() 
        => new FileGenerator(logger, SampleStrings);
    public IFileGenerator CreateOptimizedGenerator(int bufferSize = 1024 * 1024, int batchSize = 1000) 
        => new OptimizedFileGenerator(logger, SampleStrings, bufferSize, batchSize);
    public IFileGenerator CreateParallelGenerator(string outputFilePath, int chunkSize = 100 * 1024 * 1024, int maxDegreeOfParallelism = 0) 
        => new ParallelFileGenerator(logger, SampleStrings, outputFilePath, chunkSize, maxDegreeOfParallelism);
    
    public IFileGenerator CreateParallelWithChunkGenerator(int chunkSize = 100 * 1024 * 1024, int maxDegreeOfParallelism = 0) 
        => new ParallelFileGeneratorWithChunkProcessor(logger, SampleStrings, chunkSize, maxDegreeOfParallelism);
}