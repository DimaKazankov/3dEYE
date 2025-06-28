using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using _3dEYE.Generator;
using _3dEYE.Generator.Algorithms;

namespace _3dEYE.Benchmark;

[MemoryDiagnoser]
[SimpleJob]
public class FileGeneratorBenchmarkProgram
{
    private IFileGenerator _fileGenerator = null!;
    private ILogger<FileGeneratorBenchmarkProgram>? _logger;
    private const long OneGB = 1024L * 1024L * 1024L; // 1 GB in bytes

    [GlobalSetup]
    public void Setup()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        _logger = loggerFactory.CreateLogger<FileGeneratorBenchmarkProgram>();
        var factory = new FileGeneratorFactory(_logger);
        _fileGenerator = factory.GetFileGenerator();
    }

    public async Task WithCleanup(Func<string, Task> action, string fileName)
    {
        var filePath = Path.Combine(Path.GetTempPath(), fileName);
        await action(filePath);
        if (!File.Exists(filePath)) return;
        try
        {
            File.Delete(filePath);
        }
        catch (Exception ex)
        {
            _logger?.LogError("Warning: Could not delete temporary file {FilePath}: {Message}", filePath, ex.Message);
        }
    }

    [Benchmark]
    public async Task Generate1GBFile()
    {
        await WithCleanup(async filePath =>
        {
            await _fileGenerator.GenerateFileAsync(filePath, OneGB);
        }, $"benchmark_file_{Guid.NewGuid()}.txt");
    }

    [Benchmark]
    public async Task Generate100MBFile()
    {
        await WithCleanup(async filePath =>
        {
            await _fileGenerator.GenerateFileAsync(filePath, 100 * 1024 * 1024); // 100 MB
        }, $"benchmark_100mb_{Guid.NewGuid()}.txt");
    }

    [Benchmark]
    public async Task Generate10MBFile()
    {
        await WithCleanup(async filePath =>
        {
            await _fileGenerator.GenerateFileAsync(filePath, 10 * 1024 * 1024); // 10 MB
        }, $"benchmark_10mb_{Guid.NewGuid()}.txt");
    }
}