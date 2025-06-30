namespace _3dEYE.Sorter;

/// <summary>
/// Interface for sorting large files that don't fit in memory
/// </summary>
public interface ISorter
{
    /// <summary>
    /// Sorts a large file using external merge sort
    /// </summary>
    /// <param name="inputFilePath">Path to the input file to sort</param>
    /// <param name="outputFilePath">Path where the sorted file will be written</param>
    /// <param name="comparer">Optional custom comparer for sorting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task that completes when sorting is finished</returns>
    Task SortAsync(string inputFilePath, string outputFilePath, IComparer<string>? comparer = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sorts a large file using external merge sort with custom buffer size
    /// </summary>
    /// <param name="inputFilePath">Path to the input file to sort</param>
    /// <param name="outputFilePath">Path where the sorted file will be written</param>
    /// <param name="bufferSizeBytes">Size of memory buffer to use for sorting chunks</param>
    /// <param name="comparer">Optional custom comparer for sorting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task that completes when sorting is finished</returns>
    Task SortAsync(string inputFilePath, string outputFilePath, long bufferSizeBytes, IComparer<string>? comparer = null, CancellationToken cancellationToken = default);
} 