using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Logging;
using _3dEYE.Sorter;
using _3dEYE.Sorter.Models;
using _3dEYE.Generator.Algorithms;
using System.Text;

namespace _3dEYE.Benchmark;

[MemoryDiagnoser]
[SimpleJob]
public class StringFirstPartitionSorterBenchmarks
{
    private readonly ILogger _logger;
    private readonly string _testDataDir;
    private readonly LineDataComparer _comparer;

    public StringFirstPartitionSorterBenchmarks()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });
        _logger = loggerFactory.CreateLogger<StringFirstPartitionSorterBenchmarks>();
        _testDataDir = Path.Combine(Path.GetTempPath(), "3dEYE_Benchmark_Data");
        _comparer = new LineDataComparer();
        
        // Ensure test data directory exists
        Directory.CreateDirectory(_testDataDir);
    }

    [Params(104857600, 1073741824)] // 100MB, 1GB
    public long FileSizeBytes { get; set; }

    [Params(1048576, 10485760)] // 1MB, 10MB
    public int BufferSizeBytes { get; set; }

    [Params(50000, 100000)] // 50K, 100K lines in memory
    public int MaxMemoryLines { get; set; }

    [Params(4, 8)] // 4, 8 threads
    public int MaxDegreeOfParallelism { get; set; }

    private string InputFilePath => Path.Combine(_testDataDir, $"test_data_{FileSizeBytes}.txt");
    private string OutputFilePath => Path.Combine(_testDataDir, $"sorted_output_{FileSizeBytes}.txt");

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        // Clean up output file before each run
        if (File.Exists(OutputFilePath))
            File.Delete(OutputFilePath);

        // Generate test data if it doesn't exist
        if (!File.Exists(InputFilePath))
        {
            await GenerateTestFile(InputFilePath, (int)FileSizeBytes);
        }
    }

    [Benchmark]
    public async Task Sort()
    {
        var sorter = new StringFirstPartitionSorter(
            _logger, 
            MaxMemoryLines, 
            BufferSizeBytes, 
            MaxDegreeOfParallelism);

        await sorter.SortAsync(InputFilePath, OutputFilePath, _comparer);
    }

    [Benchmark]
    public async Task GetStatistics()
    {
        var sorter = new StringFirstPartitionSorter(
            _logger, 
            MaxMemoryLines, 
            BufferSizeBytes, 
            MaxDegreeOfParallelism);

        await sorter.GetSortStatisticsAsync(InputFilePath, BufferSizeBytes);
    }

    private async Task GenerateTestFile(string filePath, int targetSizeBytes)
    {
        var generator = new FileGeneratorFactory(_logger).CreateParallelGenerator(200 * 1024 * 1024, Environment.ProcessorCount);
        await generator.GenerateFileAsync(filePath, targetSizeBytes);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        // Clean up output files
        if (File.Exists(OutputFilePath))
        {
            File.Delete(OutputFilePath);
        }
    }
} 