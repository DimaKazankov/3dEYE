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

        try
        {
            logger.LogInformation("Starting comprehensive benchmarks...");
            
            // Run File Generator benchmarks
            var fileGeneratorSummary = BenchmarkRunner.Run<FileGeneratorBenchmarks>();
            logger.LogInformation("FileGenerator benchmarks completed!");
            logger.LogInformation("Results saved to: {ResultsPath}", fileGeneratorSummary.ResultsDirectoryPath);
            //
            // // Run External Merge Sorter benchmarks
            // var externalMergeSummary = BenchmarkRunner.Run<ExternalMergeSorterBenchmarks>();
            // logger.LogInformation("ExternalMergeSorter benchmarks completed!");
            // logger.LogInformation("Results saved to: {ResultsPath}", externalMergeSummary.ResultsDirectoryPath);
            //
            // // Run Parallel External Merge Sorter benchmarks
            // var parallelMergeSummary = BenchmarkRunner.Run<ParallelExternalMergeSorterBenchmarks>();
            // logger.LogInformation("ParallelExternalMergeSorter benchmarks completed!");
            // logger.LogInformation("Results saved to: {ResultsPath}", parallelMergeSummary.ResultsDirectoryPath);
            //
            // // Run Streaming Sorter benchmarks
            // var streamingSummary = BenchmarkRunner.Run<StreamingSorterBenchmarks>();
            // logger.LogInformation("StreamingSorter benchmarks completed!");
            // logger.LogInformation("Results saved to: {ResultsPath}", streamingSummary.ResultsDirectoryPath);
            //
            // // Run New Streaming Sorter benchmarks
            var newStreamingSummary = BenchmarkRunner.Run<ThreeDEyeSorterBenchmarks>();
            logger.LogInformation("ThreeDEyeSorterBenchmarks benchmarks completed!");
            // logger.LogInformation("Results saved to: {ResultsPath}", newStreamingSummary.ResultsDirectoryPath);

            logger.LogInformation("=== ALL BENCHMARK RESULTS ===");
            DisplayBenchmarkResults(fileGeneratorSummary, logger, "FileGenerator");
            // DisplayBenchmarkResults(externalMergeSummary, logger, "ExternalMergeSorter");
            // DisplayBenchmarkResults(parallelMergeSummary, logger, "ParallelExternalMergeSorter");
            // DisplayBenchmarkResults(streamingSummary, logger, "StreamingSorter");
            DisplayBenchmarkResults(newStreamingSummary, logger, "ThreeDEyeSorterBenchmarks");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Benchmark failed with error: {Message}", ex.Message);
            throw;
        }
    }

    private static void DisplayBenchmarkResults(BenchmarkDotNet.Reports.Summary summary, ILogger logger, string benchmarkName)
    {
        logger.LogInformation("=== {BenchmarkName} RESULTS ===", benchmarkName);
        
        foreach (var report in summary.Reports)
        {
            var methodName = report.BenchmarkCase.Descriptor.WorkloadMethod.Name;
            var category = report.BenchmarkCase.Descriptor.Categories.FirstOrDefault() ?? "General";
            var stats = report.ResultStatistics;
            var gcStats = report.GcStats;

            logger.LogInformation("Benchmark: {BenchmarkName} [{Category}]", methodName, category);
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