namespace _3dEYE.Sorter.Web;

public class ExternalMergeSortMergeOptions
{
    /// <summary>
    /// How many files we will process per run
    /// </summary>
    public int FilesPerRun { get; init; } = 10;
    /// <summary>
    /// Buffer size (in bytes) for input StreamReaders
    /// </summary>
    public int InputBufferSize { get; init; } = 65536;
    /// <summary>
    /// Buffer size (in bytes) for output StreamWriter
    /// </summary>
    public int OutputBufferSize { get; init; } = 65536;

    public IProgress<double> ProgressHandler { get; init; } = null!;
}