namespace _3dEYE.Sorter.Web;

public class ExternalMergeSortSortOptions
{
    public IComparer<string> Comparer { get; init; } = Comparer<string>.Default;
    public int InputBufferSize { get; init; } = 65536;
    public int OutputBufferSize { get; init; } = 65536;
    public IProgress<double> ProgressHandler { get; init; } = null!;
}