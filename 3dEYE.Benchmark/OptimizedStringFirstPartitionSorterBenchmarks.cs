using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Extensions.Logging;
using _3dEYE.Sorter.Web;
using _3dEYE.Sorter.Models;
using _3dEYE.Generator.Algorithms;
using System.Text;
using BenchmarkDotNet.Jobs;

namespace _3dEYE.Benchmark;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90, warmupCount: 1, iterationCount: 3)]
public class OptimizedStringFirstPartitionSorterBenchmarks
{
    private string _testDirectory = null!;
    private string _inputFile = null!;
    private string _outputFile = null!;
    private ExternalMergeSorter _sorter = null!;
    private ILogger<ExternalMergeSorter> _logger = null!;

    [Params(1024 * 1024 * 1024)] // 1GB for testing 100GB file scenarios
    public long FileSizeBytes { get; set; }

    [Params(10 * 1024 * 1024)] // 1MB buffer
    public int BufferSizeBytes { get; set; }

    // [Params(50000)] // 50K lines in memory
    // public int MaxMemoryLines { get; set; }
    //
    // [Params(4)] // 4 threads
    // public int MaxDegreeOfParallelism { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        try
        {
            // Use home directory instead of /tmp to avoid disk space issues
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _testDirectory = Path.Combine(homeDir, ".3dEYE_benchmarks", $"OptimizedStringFirstPartitionBenchmark_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_testDirectory);

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Warning);
            });
            
            _logger = loggerFactory.CreateLogger<ExternalMergeSorter>();

            _inputFile = Path.Combine(_testDirectory, "benchmark_input.txt");
            _outputFile = Path.Combine(_testDirectory, "benchmark_output.txt");

            // Generate test data using ParallelFileGenerator with smaller chunks
            await GenerateTestFile(_inputFile, (int)FileSizeBytes);

            // Initialize sorter with more conservative settings
            
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to setup benchmark");
            throw;
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        try
        {
            if (Directory.Exists(_testDirectory)) 
                Directory.Delete(_testDirectory, true);
        }
        catch (Exception ex)
        { 
            _logger?.LogWarning(ex, "Failed to cleanup test directory");
        }
    }

    // [Benchmark]
    // [BenchmarkCategory("OptimizedStringFirstPartition")]
    // public async Task Sort()
    // {
    //     try
    //     {
    //         var source = File.OpenRead(_inputFile);
    //         var target = File.OpenWrite(_outputFile);
    //         using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(100)); // 10 minute timeout
    //         await _sorter.Sort(source, target, cts.Token);
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger?.LogError(ex, "Sort benchmark failed");
    //         throw;
    //     }
    // }

    [Benchmark]
    [BenchmarkCategory("OptimizedStringFirstPartition")]
    public async Task Sort()
    {
        try
        {
            var source = File.OpenRead(_inputFile);
            var target = File.OpenWrite(_outputFile);
            _sorter = new ExternalMergeSorter(new ExternalMergeSorterOptions
            {
                Sort = new ExternalMergeSortSortOptions
                {
                    Comparer = new LineComparer(),
                    InputBufferSize = BufferSizeBytes,
                    OutputBufferSize = BufferSizeBytes,
                },
                Split = new ExternalMergeSortSplitOptions
                {
                    FileSize = 32 * 1024 * 1024
                },
                FileLocation = _testDirectory
            });
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(100)); // 10 minute timeout
            await _sorter.Sort(source, target, cts.Token);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "GetStatistics benchmark failed");
            throw;
        }
    }

    private async Task GenerateTestFile(string filePath, int targetSizeBytes)
    {
        try
        {
            // Use smaller chunk size to prevent memory issues
            var chunkSize = 50 * 1024 * 1024; // 50MB chunks instead of 200MB
            var maxThreads = Math.Min(Environment.ProcessorCount, 4); // Limit to 4 threads
            
            var generator = new FileGeneratorFactory(_logger).CreateParallelGenerator(filePath, chunkSize, maxThreads);
            await generator.GenerateFileAsync(filePath, targetSizeBytes);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to generate test file");
            throw;
        }
    }
} 