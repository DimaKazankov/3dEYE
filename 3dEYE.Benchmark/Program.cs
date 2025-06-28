using BenchmarkDotNet.Running;
using Microsoft.Extensions.Logging;

namespace _3dEYE.Benchmark;

public class Program
{
    public static void Main(string[] args)
    {
        // Create a logger factory for the program
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<Program>();

        logger.LogInformation("Starting FileGenerator Benchmark...");
        logger.LogInformation("This will test file generation performance with different file sizes.");
        logger.LogWarning("Note: The 1GB benchmark may take significant time and disk space.");

        // Run the benchmark
        var summary = BenchmarkRunner.Run<FileGeneratorBenchmarkProgram>();
        
        logger.LogInformation("Benchmark completed!");
        logger.LogInformation("Results saved to: {ResultsPath}", summary.ResultsDirectoryPath);
        
        // Display some key metrics
        foreach (var report in summary.Reports)
        {
            var methodName = report.BenchmarkCase.Descriptor.WorkloadMethod.Name;
            var stats = report.ResultStatistics;
            var gcStats = report.GcStats;

            logger.LogInformation("Benchmark: {BenchmarkName}", methodName);
            if (stats != null)
            {
                logger.LogInformation("  Mean Time: {Mean:F2} ns", stats.Mean);
                logger.LogInformation("  StdDev: {StdDev:F2} ns", stats.StandardDeviation);
            }
            
            logger.LogInformation("  Gen0 Collections: {Gen0}", gcStats.Gen0Collections);
            logger.LogInformation("  Gen1 Collections: {Gen1}", gcStats.Gen1Collections);
            logger.LogInformation("  Gen2 Collections: {Gen2}", gcStats.Gen2Collections);
        }
    }
}