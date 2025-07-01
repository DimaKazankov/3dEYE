using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Logging;
using _3dEYE.Sorter;
using _3dEYE.Sorter.Models;
using _3dEYE.Generator.Algorithms;
using System.Text;
using BenchmarkDotNet.Jobs;

namespace _3dEYE.Benchmark;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90, warmupCount: 1, iterationCount: 3)]
public class StringFirstPartitionSorterBenchmarks
{
    private string _testDirectory = null!;
    private string _inputFile = null!;
    private string _outputFile = null!;
    private StringFirstPartitionSorter _sorter = null!;
    private ILogger<StringFirstPartitionSorter> _logger = null!;

    [Params(104857600, 1073741824)] // 100MB, 1GB
    public long FileSizeBytes { get; set; }

    [Params(1048576, 10485760)] // 1MB, 10MB
    public int BufferSizeBytes { get; set; }

    [Params(50000, 100000)] // 50K, 100K lines in memory
    public int MaxMemoryLines { get; set; }

    [Params(4, 8)] // 4, 8 threads
    public int MaxDegreeOfParallelism { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"StringFirstPartitionBenchmark_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });
        
        _logger = loggerFactory.CreateLogger<StringFirstPartitionSorter>();

        _inputFile = Path.Combine(_testDirectory, "benchmark_input.txt");
        _outputFile = Path.Combine(_testDirectory, "benchmark_output.txt");

        // Generate test data using ParallelFileGenerator
        await GenerateTestFile(_inputFile, (int)FileSizeBytes);

        // Initialize sorter
        _sorter = new StringFirstPartitionSorter(_logger, MaxMemoryLines, BufferSizeBytes, MaxDegreeOfParallelism);
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
    [BenchmarkCategory("StringFirstPartition")]
    public async Task Sort()
    {
        await _sorter.SortAsync(_inputFile, _outputFile, new LineDataComparer());
    }

    [Benchmark]
    [BenchmarkCategory("StringFirstPartition")]
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