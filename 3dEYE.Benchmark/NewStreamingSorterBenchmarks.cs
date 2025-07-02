using _3dEYE.Generator.Algorithms;
using _3dEYE.Sorter;
using _3dEYE.Sorter.Models;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.Logging;

namespace _3dEYE.Benchmark;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90, warmupCount: 1, iterationCount: 1)]
public class NewStreamingSorterBenchmarks
{
    private string _testDirectory = null!;
    private string _inputFile = null!;
    private string _outputFile = null!;
    private ThreeDEyeSorter _sorter = null!;
    private ILogger<StreamingSorter> _logger = null!;
    
    [GlobalSetup]
    public async Task Setup()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"StreamingBenchmark_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDirectory);

        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Warning);
        });
        
        _logger = loggerFactory.CreateLogger<StreamingSorter>();

        _inputFile = Path.Combine(_testDirectory, "benchmark_input.txt");
        _outputFile = Path.Combine(_testDirectory, "benchmark_output.txt");

        // Generate test data using ParallelFileGenerator
        await GenerateTestFile(_inputFile, 1024 * 1024 * 1024);

        // Initialize sorter
        _sorter = new ThreeDEyeSorter();
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
    [BenchmarkCategory("Streaming")]
    public async Task Sort()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await _sorter.SortAsync(_inputFile, _outputFile, _testDirectory);
    }

    private async Task GenerateTestFile(string filePath, int targetSizeBytes)
    {
        var generator = new FileGeneratorFactory(_logger).CreateParallelGenerator(filePath, 200 * 1024 * 1024, Environment.ProcessorCount);
        await generator.GenerateFileAsync(filePath, targetSizeBytes);
    }
}