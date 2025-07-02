using _3dEYE.Sorter;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace _3dEYE.Benchmark;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90, warmupCount: 1, iterationCount: 1)]
public class NewStreamingSorterBenchmarks
{
    private TestFileManager<ThreeDEyeSorter> _fileManager = null!;
    private ThreeDEyeSorter _sorter = null!;

    [GlobalSetup]
    public async Task Setup()
    {
        _fileManager = new TestFileManager<ThreeDEyeSorter>("ThreeDEyeSorterBenchmark");
        await _fileManager.InputFile.GenerateTestFile(_fileManager.Logger);
        _sorter = new ThreeDEyeSorter();
    }

    [GlobalCleanup]
    public void Cleanup() => _fileManager.Dispose();

    [Benchmark]
    [BenchmarkCategory("ThreeDEyeSorter")]
    public async Task Sort()
    {
        await _sorter.SortAsync(_fileManager.InputFile, _fileManager.OutputFile, _fileManager.TestDirectory);
    }
}