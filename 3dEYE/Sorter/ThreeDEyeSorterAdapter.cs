using _3dEYE.Sorter.Models;

namespace _3dEYE.Sorter;

/// <summary>
/// Adapter class that wraps ThreeDEyeSorter to implement ISorter interface
/// </summary>
public class ThreeDEyeSorterAdapter : ISorter
{
    private readonly ThreeDEyeSorter _sorter;

    public ThreeDEyeSorterAdapter()
    {
        _sorter = new ThreeDEyeSorter();
    }

    public async Task SortAsync(string inputFilePath, string outputFilePath, IComparer<LineData> comparer, CancellationToken cancellationToken = default)
    {
        // Check if input file is empty
        var fileInfo = new FileInfo(inputFilePath);
        if (fileInfo.Length == 0)
        {
            // Create empty output file
            await File.WriteAllTextAsync(outputFilePath, "", cancellationToken).ConfigureAwait(false);
            return;
        }

        // ThreeDEyeSorter doesn't use the comparer parameter, it has its own internal comparer
        var tempDir = Path.Combine(Path.GetTempPath(), $"threedeye_sort_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            await _sorter.SortAsync(inputFilePath, outputFilePath, tempDir);
        }
        finally
        {
            // Clean up temporary directory
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    public async Task SortAsync(string inputFilePath, string outputFilePath, int bufferSizeBytes, IComparer<LineData> comparer, CancellationToken cancellationToken = default)
    {
        // ThreeDEyeSorter doesn't use bufferSizeBytes parameter, it has its own internal buffer management
        await SortAsync(inputFilePath, outputFilePath, comparer, cancellationToken);
    }
} 