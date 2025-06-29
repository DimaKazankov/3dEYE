using BenchmarkDotNet.Running;
using Microsoft.Extensions.Logging;

namespace _3dEYE.Benchmark;

public class Program
{
    public static void Main(string[] args)
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        var logger = loggerFactory.CreateLogger<Program>();

        logger.LogInformation("Running parallel FileGenerator comparison benchmark...");
        var parallelSummary = BenchmarkRunner.Run<FileGeneratorBenchmarks>();
        
        logger.LogInformation("Parallel benchmark completed!");
        logger.LogInformation("Results saved to: {ResultsPath}", parallelSummary.ResultsDirectoryPath);

        logger.LogInformation("=== PARALLEL GENERATOR COMPARISON ===");
        DisplayBenchmarkResults(parallelSummary, logger);
    }

    private static void DisplayBenchmarkResults(BenchmarkDotNet.Reports.Summary summary, ILogger logger)
    {
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