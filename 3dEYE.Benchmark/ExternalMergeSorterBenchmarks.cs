using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using _3dEYE.Sorter;
using _3dEYE.Sorter.Models;

namespace _3dEYE.Benchmark;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90, warmupCount: 1, iterationCount: 1)]
public class ExternalMergeSorterBenchmarks
{
    [Params(1024 * 1024, 10 * 1024 * 1024)] // 1MB buffer
    public int BufferSizeBytes { get; set; }

    private TestFileManager<ExternalMergeSorter> _fileManager = null!;
    private ExternalMergeSorter _sorter = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        _fileManager = new TestFileManager<ExternalMergeSorter>("ExternalMergeBenchmark");
        await _fileManager.InputFile.GenerateTestFile(_fileManager.Logger);
        _sorter = new ExternalMergeSorter(_fileManager.Logger, BufferSizeBytes, _fileManager.TestDirectory);
    }

    [GlobalCleanup]
    public void Cleanup() => _fileManager.Dispose();
    
    [Benchmark]
    [BenchmarkCategory("ExternalMerge")]
    public async Task Sort()
    {
        await _sorter.SortAsync(_fileManager.InputFile, _fileManager.OutputFile, bufferSizeBytes: BufferSizeBytes, new LineDataComparer());
    }
}