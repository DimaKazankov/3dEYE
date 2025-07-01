namespace _3dEYE.Sorter.Web;

public class ExternalMergeSorterOptions
{
    public string FileLocation { get; init; } = "c:\\temp\\files";
    public ExternalMergeSortSplitOptions Split { get; init; } = new();
    public ExternalMergeSortSortOptions Sort { get; init; } = new();
    public ExternalMergeSortMergeOptions Merge { get; init; } = new();
}