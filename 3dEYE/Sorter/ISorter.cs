using _3dEYE.Sorter.Models;

namespace _3dEYE.Sorter;

public interface ISorter
{
    Task SortAsync(string inputFilePath, string outputFilePath, IComparer<LineData> comparer, CancellationToken cancellationToken = default);
    
    Task SortAsync(string inputFilePath, string outputFilePath, int bufferSizeBytes, IComparer<LineData> comparer, CancellationToken cancellationToken = default);
} 