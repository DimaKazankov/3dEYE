using _3dEYE.Sorter.Models;

namespace _3dEYE.Sorter;

public class ThreeDEyeSorterAdapter : ISorter
{
    private readonly ThreeDEyeSorter _sorter = new();

    public async Task SortAsync(string inputFilePath, string outputFilePath, IComparer<LineData> comparer, CancellationToken cancellationToken = default)
    {
        var fileInfo = new FileInfo(inputFilePath);
        if (fileInfo.Length == 0)
        {
            await File.WriteAllTextAsync(outputFilePath, "", cancellationToken).ConfigureAwait(false);
            return;
        }

        var tempDir = Path.Combine(Path.GetTempPath(), $"threedeye_sort_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            await _sorter.SortAsync(inputFilePath, outputFilePath, tempDir);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    public async Task SortAsync(string inputFilePath, string outputFilePath, int bufferSizeBytes, IComparer<LineData> comparer, CancellationToken cancellationToken = default)
    {
        await SortAsync(inputFilePath, outputFilePath, comparer, cancellationToken);
    }
} 