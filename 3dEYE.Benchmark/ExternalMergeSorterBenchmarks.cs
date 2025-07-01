using _3dEYE.Generator.Algorithms;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.Logging;
using _3dEYE.Sorter;
using _3dEYE.Sorter.Models;

namespace _3dEYE.Benchmark;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90, warmupCount: 1, iterationCount: 3)]
public class ExternalMergeSorterBenchmarks
{
    private string _testDirectory = null!;
    private string _inputFile = null!;
    private string _outputFile = null!;
    private ExternalMergeSorter _sorter = null!;
    private ILogger<ExternalMergeSorter> _logger = null!;

    [Params(100 * 1024 * 1024, 1024 * 1024 * 1024)] // 100MB, 1GB
    public int FileSizeBytes { get; set; }

    [Params(1024 * 1024, 10 * 1024 * 1024)] // 1MB buffer
    public int BufferSizeBytes { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"ExternalMergeBenchmark_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });
        
        _logger = loggerFactory.CreateLogger<ExternalMergeSorter>();

        _inputFile = Path.Combine(_testDirectory, "benchmark_input.txt");
        _outputFile = Path.Combine(_testDirectory, "benchmark_output.txt");

        // Generate test data using ParallelFileGenerator
        await GenerateTestFile(_inputFile, FileSizeBytes);

        // Initialize sorter
        _sorter = new ExternalMergeSorter(_logger, BufferSizeBytes, _testDirectory);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        try
        {
            if (Directory.Exists(_testDirectory)) 
                Directory.Delete(_testDirectory, true);
        }
        catch
        { 
            // Ignore cleanup errors
        }
    }

    [Benchmark]
    [BenchmarkCategory("ExternalMerge")]
    public async Task Sort()
    {
        await _sorter.SortAsync(_inputFile, _outputFile, bufferSizeBytes: BufferSizeBytes, new LineDataComparer());
    }

    [Benchmark]
    [BenchmarkCategory("ExternalMerge")]
    public async Task GetStatistics()
    {
        await _sorter.GetSortStatisticsAsync(_inputFile, BufferSizeBytes);
    }

    private async Task GenerateTestFile(string filePath, int targetSizeBytes)
    {
        var generator = new FileGeneratorFactory(_logger).CreateParallelGenerator(filePath, 200 * 1024 * 1024, Environment.ProcessorCount);
        await generator.GenerateFileAsync(filePath, targetSizeBytes);
    }
}