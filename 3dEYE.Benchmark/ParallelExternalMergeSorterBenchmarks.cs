using _3dEYE.Sorter;
using _3dEYE.Sorter.Models;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace _3dEYE.Benchmark;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90, warmupCount: 1, iterationCount: 1)]
public class ParallelExternalMergeSorterBenchmarks
{
    [Params(1024 * 1024, 10 * 1024 * 1024)] // 1MB buffer
    public int BufferSizeBytes { get; set; }

    [Params(4, 8)] // Different parallelism levels
    public int MaxDegreeOfParallelism { get; set; }
 
    private TestFileManager<ParallelExternalMergeSorter> _fileManager = null!;
    private ParallelExternalMergeSorter _sorter = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        _fileManager = new TestFileManager<ParallelExternalMergeSorter>("ParallelExternalMergeSorterBenchmark");
        await _fileManager.InputFile.GenerateTestFile(_fileManager.Logger);
        _sorter = new ParallelExternalMergeSorter(_fileManager.Logger, BufferSizeBytes, _fileManager.TestDirectory, MaxDegreeOfParallelism);
    }

    [GlobalCleanup]
    public void Cleanup() => _fileManager.Dispose();
    
    [Benchmark]
    [BenchmarkCategory("ParallelExternalMerge")]
    public async Task Sort()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await _sorter.SortAsync(_fileManager.InputFile, _fileManager.OutputFile, 
            bufferSizeBytes: BufferSizeBytes, new LineDataComparer(), cancellationToken: cts.Token);
    }
}