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

    public IFileGenerator CreateGeneratorForFileSize(long fileSizeInBytes, string outputFilePath)
    {
        const long oneGb = 1024L * 1024 * 1024;
        const long tenGb = 10L * oneGb;
        const long hundredGb = 100L * oneGb;
        switch (fileSizeInBytes)
        {
            case < oneGb:
                logger.LogInformation("File size {FileSize} bytes (< 1GB), using basic FileGenerator", fileSizeInBytes);
                return CreateBasicGenerator();
            case < tenGb:
                logger.LogInformation("File size {FileSize} bytes (1-10GB), using optimized FileGenerator", fileSizeInBytes);
                return CreateOptimizedGenerator(4 * 1024 * 1024, 2000); // 4MB buffer, 2000 batch size
            case < hundredGb:
                logger.LogInformation("File size {FileSize} bytes (10-100GB), using parallel FileGenerator", fileSizeInBytes);
                return CreateParallelGenerator(outputFilePath, 200 * 1024 * 1024, 4); // 200MB chunks, 4 threads
            default:
                logger.LogInformation("File size {FileSize} bytes (> 100GB), using ultra-large parallel FileGenerator", fileSizeInBytes);
                return CreateParallelGenerator(outputFilePath, 500 * 1024 * 1024, 8); // 500MB chunks, 8 threads
        }
    }
}