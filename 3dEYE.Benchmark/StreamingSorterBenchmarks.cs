using _3dEYE.Sorter;
using _3dEYE.Sorter.Models;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace _3dEYE.Benchmark;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90, warmupCount: 1, iterationCount: 1)]
public class StreamingSorterBenchmarks
{
    private TestFileManager<StreamingSorter> _fileManager = null!;
    private StreamingSorter _sorter = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        _fileManager = new TestFileManager<StreamingSorter>("StreamingBenchmark");
        await _fileManager.InputFile.GenerateTestFile(_fileManager.Logger);
        _sorter = new StreamingSorter(_fileManager.Logger, bufferSize: 10 * 1024 * 1024);
    }

    [GlobalCleanup]
    public void Cleanup() => _fileManager.Dispose();

    [Benchmark]
    [BenchmarkCategory("Streaming")]
    public async Task Sort()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await _sorter.SortAsync(_fileManager.InputFile, _fileManager.OutputFile, new LineDataComparer(), cancellationToken: cts.Token);
    }
}