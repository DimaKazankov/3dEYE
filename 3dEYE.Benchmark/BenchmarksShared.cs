using _3dEYE.Generator.Algorithms;
using Microsoft.Extensions.Logging;

namespace _3dEYE.Benchmark;

public static class BenchmarksShared
{
    public static async Task GenerateTestFile(this string filePath, ILogger logger, int targetSizeBytes = 1024 * 1024 * 1024)
    {
        var generator = new FileGeneratorFactory(logger).CreateParallelGenerator(filePath, 200 * 1024 * 1024, Environment.ProcessorCount);
        await generator.GenerateFileAsync(filePath, targetSizeBytes);
    }
}