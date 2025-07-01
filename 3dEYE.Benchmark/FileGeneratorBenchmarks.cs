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
    private const long OneGb = 1024L * 1024L * 1024L; // 1 GB in bytes
    
    private IFileGenerator _originalGenerator = null!;
    private IFileGenerator _optimizedGenerator = null!;
    private IFileGenerator _parallelGenerator = null!;
    private IFileGenerator _largeParallelGenerator = null!;
    private ILogger<FileGeneratorBenchmarks>? _logger;

    [GlobalSetup]
    public void Setup()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        _logger = loggerFactory.CreateLogger<FileGeneratorBenchmarks>();
        
        var fileGeneratorFactory = new FileGeneratorFactory(_logger);
        
        _originalGenerator = fileGeneratorFactory.CreateBasicGenerator();
        _optimizedGenerator = fileGeneratorFactory.CreateOptimizedGenerator();
        _parallelGenerator = fileGeneratorFactory.CreateParallelGenerator(Path.GetTempFileName());
        _largeParallelGenerator = fileGeneratorFactory.CreateParallelGenerator(Path.GetTempFileName(), 200 * 1024 * 1024, 4);
    }

    [Benchmark]
    public async Task Original_Generate1GBFile()
    {
        await FileHelper.WithCleanup(async filePath =>
        {
            await _originalGenerator.GenerateFileAsync(filePath, OneGb);
        }, $"original_1gb_{Guid.NewGuid()}.txt", _logger);
    }

    [Benchmark]
    public async Task Optimized_Generate1GBFile()
    {
        await FileHelper.WithCleanup(async filePath =>
        {
            await _optimizedGenerator.GenerateFileAsync(filePath, OneGb);
        }, $"optimized_1gb_{Guid.NewGuid()}.txt", _logger);
    }

    [Benchmark]
    public async Task Parallel_Generate1GBFile()
    {
        await FileHelper.WithCleanup(async filePath =>
        {
            await _parallelGenerator.GenerateFileAsync(filePath, OneGb);
        }, $"parallel_1gb_{Guid.NewGuid()}.txt", _logger);
    }

    [Benchmark]
    public async Task LargeParallel_Generate1GBFile()
    {
        await FileHelper.WithCleanup(async filePath =>
        {
            await _largeParallelGenerator.GenerateFileAsync(filePath, OneGb);
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

    [Benchmark]
    public async Task Original_Generate10MBFile()
    {
        await FileHelper.WithCleanup(async filePath =>
        {
            await _originalGenerator.GenerateFileAsync(filePath, 10 * 1024 * 1024);
        }, $"original_10mb_{Guid.NewGuid()}.txt", _logger);
    }

    [Benchmark]
    public async Task Optimized_Generate10MBFile()
    {
        await FileHelper.WithCleanup(async filePath =>
        {
            await _optimizedGenerator.GenerateFileAsync(filePath, 10 * 1024 * 1024);
        }, $"optimized_10mb_{Guid.NewGuid()}.txt", _logger);
    }

    [Benchmark]
    public async Task Parallel_Generate10MBFile()
    {
        await FileHelper.WithCleanup(async filePath =>
        {
            await _parallelGenerator.GenerateFileAsync(filePath, 10 * 1024 * 1024);
        }, $"parallel_10mb_{Guid.NewGuid()}.txt", _logger);
    }
} 