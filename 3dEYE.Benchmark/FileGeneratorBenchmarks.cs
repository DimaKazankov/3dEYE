using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using _3dEYE.Generator;
using _3dEYE.Generator.Algorithms;
using _3dEYE.Helpers;

namespace _3dEYE.Benchmark;

[MemoryDiagnoser]
[SimpleJob]
public class FileGeneratorBenchmarks
{
    private IFileGenerator _originalGenerator = null!;
    private IFileGenerator _optimizedGenerator = null!;
    private IFileGenerator _parallelGenerator = null!;
    private IFileGenerator _largeParallelGenerator = null!;
    private ILogger<FileGeneratorBenchmarks>? _logger;
    private const long OneGB = 1024L * 1024L * 1024L; // 1 GB in bytes

    [GlobalSetup]
    public void Setup()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        _logger = loggerFactory.CreateLogger<FileGeneratorBenchmarks>();
        
        var originalFactory = new FileGeneratorFactory(_logger);
        var optimizedFactory = new OptimizedFileGeneratorFactory(_logger);
        var parallelFactory = new ParallelFileGeneratorFactory(_logger);
        
        _originalGenerator = originalFactory.GetFileGenerator();
        _optimizedGenerator = optimizedFactory.GetOptimizedFileGenerator();
        _parallelGenerator = parallelFactory.GetBalancedParallelGenerator();
        _largeParallelGenerator = parallelFactory.GetLargeFileParallelGenerator();
    }

    [Benchmark]
    public async Task Original_Generate1GBFile()
    {
        await FileHelper.WithCleanup(async filePath =>
        {
            await _originalGenerator.GenerateFileAsync(filePath, OneGB);
        }, $"original_1gb_{Guid.NewGuid()}.txt", _logger);
    }

    [Benchmark]
    public async Task Optimized_Generate1GBFile()
    {
        await FileHelper.WithCleanup(async filePath =>
        {
            await _optimizedGenerator.GenerateFileAsync(filePath, OneGB);
        }, $"optimized_1gb_{Guid.NewGuid()}.txt", _logger);
    }

    [Benchmark]
    public async Task Parallel_Generate1GBFile()
    {
        await FileHelper.WithCleanup(async filePath =>
        {
            await _parallelGenerator.GenerateFileAsync(filePath, OneGB);
        }, $"parallel_1gb_{Guid.NewGuid()}.txt", _logger);
    }

    [Benchmark]
    public async Task LargeParallel_Generate1GBFile()
    {
        await FileHelper.WithCleanup(async filePath =>
        {
            await _largeParallelGenerator.GenerateFileAsync(filePath, OneGB);
        }, $"large_parallel_1gb_{Guid.NewGuid()}.txt", _logger);
    }

    [Benchmark]
    public async Task Original_Generate100MBFile()
    {
        await FileHelper.WithCleanup(async filePath =>
        {
            await _originalGenerator.GenerateFileAsync(filePath, 100 * 1024 * 1024);
        }, $"original_100mb_{Guid.NewGuid()}.txt", _logger);
    }

    [Benchmark]
    public async Task Optimized_Generate100MBFile()
    {
        await FileHelper.WithCleanup(async filePath =>
        {
            await _optimizedGenerator.GenerateFileAsync(filePath, 100 * 1024 * 1024);
        }, $"optimized_100mb_{Guid.NewGuid()}.txt", _logger);
    }

    [Benchmark]
    public async Task Parallel_Generate100MBFile()
    {
        await FileHelper.WithCleanup(async filePath =>
        {
            await _parallelGenerator.GenerateFileAsync(filePath, 100 * 1024 * 1024);
        }, $"parallel_100mb_{Guid.NewGuid()}.txt", _logger);
    }
} 