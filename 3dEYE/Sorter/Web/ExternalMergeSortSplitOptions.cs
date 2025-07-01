namespace _3dEYE.Sorter.Web;

public class ExternalMergeSortSplitOptions
{
    /// <summary>
    /// Size of unsorted file (chunk) (in bytes)
    /// </summary>
    public int FileSize { get; init; } = 2 * 1024 * 1024;
    public char NewLineSeparator { get; init; } = '\n';
    public IProgress<double> ProgressHandler { get; init; } = null!;
}