using _3dEYE.Generator.Algorithms;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.Logging;
using _3dEYE.Sorter;

namespace _3dEYE.Benchmark;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90, warmupCount: 1, iterationCount: 5)]
public class ExternalMergeSorterBenchmarks
{
    private string _testDirectory = null!;
    private string _inputFile = null!;
    private string _outputFile = null!;
    private ExternalMergeSorter _sorter = null!;
    private ILogger<ExternalMergeSorter> _logger = null!;

    [Params(100 * 1024 * 1024, 1024 * 1024 * 1024)] // 100MB, 10MB (further reduced for speed)
    public int FileSizeBytes { get; set; }

    [Params(64 * 1024, 1024 * 1024, 10 * 1024 * 1024)] // 64KB, 1MB
    public int BufferSizeBytes { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"Benchmark_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });
        _logger = loggerFactory.CreateLogger<ExternalMergeSorter>();

        _inputFile = Path.Combine(_testDirectory, "benchmark_input.txt");
        _outputFile = Path.Combine(_testDirectory, "benchmark_output.txt");

        // Generate test data
        await GenerateTestFile(_inputFile, FileSizeBytes);

        _sorter = new ExternalMergeSorter(_logger, BufferSizeBytes, _testDirectory);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        try
        {
            if (Directory.Exists(_testDirectory)) Directory.Delete(_testDirectory, true);
        }
        catch
        { 
        }
    }

    [Benchmark]
    [BenchmarkCategory("Sort")]
    public async Task SortFile()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2)); // 2 minute timeout
        await _sorter.SortAsync(_inputFile, _outputFile, bufferSizeBytes: BufferSizeBytes, cancellationToken: cts.Token);
    }

    [Benchmark]
    [BenchmarkCategory("Statistics")]
    public async Task GetStatistics()
    {
        await _sorter.GetSortStatisticsAsync(_inputFile, BufferSizeBytes);
    }

    private async Task GenerateTestFile(string filePath, int targetSizeBytes)
    {
        var generator = new FileGeneratorFactory(_logger).CreateParallelGenerator(200 * 1024 * 1024, 4);
        await generator.GenerateFileAsync(filePath, targetSizeBytes);
    }
}