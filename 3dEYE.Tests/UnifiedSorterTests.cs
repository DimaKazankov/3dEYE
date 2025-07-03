using Microsoft.Extensions.Logging;
using Moq;
using _3dEYE.Sorter;
using _3dEYE.Sorter.Models;

namespace _3dEYE.Tests;

[TestFixture]
public class UnifiedSorterTests : IDisposable
{
    private readonly List<string> _tempFiles = new();
    private readonly List<string> _tempDirectories = new();

    [TestCaseSource(nameof(GetSorters))]
    public async Task SortAsync_ExampleInput_SortsCorrectly(ISorter sorter, string sorterName)
    {
        var inputFile = CreateTempFile(
            "415. Apple",
            "30432. Something something something",
            "1. Apple",
            "32. Cherry is the best",
            "2. Banana is yellow"
        );
        var outputFile = GetTempFilePath();
        await sorter.SortAsync(inputFile, outputFile, new LineDataComparer());
        var output = await File.ReadAllLinesAsync(outputFile);
        var expected = new[]
        {
            "1. Apple",
            "415. Apple",
            "2. Banana is yellow",
            "32. Cherry is the best",
            "30432. Something something something"
        };
        Assert.That(output, Is.EqualTo(expected), $"Sorter: {sorterName}");
    }

    [TestCaseSource(nameof(GetSorters))]
    public async Task SortAsync_SingleLine_OutputsSameLine(ISorter sorter, string sorterName)
    {
        // Arrange
        var inputFile = CreateTempFile("1. Apple");
        var outputFile = GetTempFilePath();

        // Act
        await sorter.SortAsync(inputFile, outputFile, new LineDataComparer());

        // Assert
        var output = await File.ReadAllTextAsync(outputFile);
        Assert.That(output.TrimEnd('\r', '\n'), Is.EqualTo("1. Apple"), $"Sorter: {sorterName}");
    }

    [TestCaseSource(nameof(GetSorters))]
    public async Task SortAsync_EmptyFile_CreatesEmptyOutput(ISorter sorter, string sorterName)
    {
        // Arrange
        var inputFile = CreateEmptyTempFile();
        var outputFile = GetTempFilePath();

        // Act
        await sorter.SortAsync(inputFile, outputFile, new LineDataComparer());

        // Assert
        Assert.That(File.Exists(outputFile), Is.True, $"Sorter: {sorterName}");
        Assert.That(new FileInfo(outputFile).Length, Is.EqualTo(0), $"Sorter: {sorterName}");
    }

    [TestCaseSource(nameof(GetSorters))]
    public async Task SortAsync_SameStringDifferentNumbers_SortsByNumber(ISorter sorter, string sorterName)
    {
        // Arrange
        var inputFile = CreateTempFile(
            "100. Same Text",
            "1. Same Text",
            "50. Same Text",
            "10. Same Text"
        );
        var outputFile = GetTempFilePath();

        // Act
        await sorter.SortAsync(inputFile, outputFile, new LineDataComparer());

        // Assert
        var output = await File.ReadAllLinesAsync(outputFile);
        var expected = new[]
        {
            "1. Same Text",
            "10. Same Text",
            "50. Same Text",
            "100. Same Text"
        };
        Assert.That(output, Is.EqualTo(expected), $"Sorter: {sorterName}");
    }

    [TestCaseSource(nameof(GetSorters))]
    public async Task SortAsync_DifferentStrings_SortsAlphabetically(ISorter sorter, string sorterName)
    {
        // Arrange
        var inputFile = CreateTempFile(
            "1. Zebra",
            "2. Apple",
            "3. Banana",
            "4. Cherry"
        );
        var outputFile = GetTempFilePath();

        // Act
        await sorter.SortAsync(inputFile, outputFile, new LineDataComparer());

        // Assert
        var output = await File.ReadAllLinesAsync(outputFile);
        var expected = new[]
        {
            "2. Apple",
            "3. Banana",
            "4. Cherry",
            "1. Zebra"
        };
        Assert.That(output, Is.EqualTo(expected), $"Sorter: {sorterName}");
    }

    [TestCaseSource(nameof(GetSorters))]
    public async Task SortAsync_LargeNumbers_HandlesCorrectly(ISorter sorter, string sorterName)
    {
        // Arrange
        var inputFile = CreateTempFile(
            "999999. Small text",
            "1. Small text",
            "1000000. Small text",
            "100. Small text"
        );
        var outputFile = GetTempFilePath();

        // Act
        await sorter.SortAsync(inputFile, outputFile, new LineDataComparer());

        // Assert
        var output = await File.ReadAllLinesAsync(outputFile);
        var expected = new[]
        {
            "1. Small text",
            "100. Small text",
            "999999. Small text",
            "1000000. Small text"
        };
        Assert.That(output, Is.EqualTo(expected), $"Sorter: {sorterName}");
    }

    [TestCaseSource(nameof(GetSorters))]
    public async Task SortAsync_EmptyStringPart_HandlesCorrectly(ISorter sorter, string sorterName)
    {
        // Arrange
        var inputFile = CreateTempFile(
            "1. ",
            "2. Apple",
            "3. ",
            "4. Banana"
        );
        var outputFile = GetTempFilePath();

        // Act
        await sorter.SortAsync(inputFile, outputFile, new LineDataComparer());

        // Assert
        var output = await File.ReadAllLinesAsync(outputFile);
        var expected = new[]
        {
            "1. ",
            "3. ",
            "2. Apple",
            "4. Banana"
        };
        Assert.That(output, Is.EqualTo(expected), $"Sorter: {sorterName}");
    }

    [TestCaseSource(nameof(GetSorters))]
    public async Task SortAsync_CaseSensitiveSorting_RespectsCase(ISorter sorter, string sorterName)
    {
        // Arrange
        var inputFile = CreateTempFile(
            "1. apple",
            "2. Apple",
            "3. APPLE",
            "4. aPPle"
        );
        var outputFile = GetTempFilePath();

        // Act
        await sorter.SortAsync(inputFile, outputFile, new LineDataComparer());

        // Assert
        var output = await File.ReadAllLinesAsync(outputFile);
        var expected = new[]
        {
            "3. APPLE",
            "2. Apple",
            "4. aPPle",
            "1. apple"
        };
        Assert.That(output, Is.EqualTo(expected), $"Sorter: {sorterName}");
    }

    [TestCaseSource(nameof(GetSorters))]
    public async Task SortAsync_AlreadySorted_ReturnsSameOrder(ISorter sorter, string sorterName)
    {
        // Arrange
        var inputFile = CreateTempFile(
            "1. Apple",
            "2. Banana",
            "3. Cherry",
            "4. Date"
        );
        var outputFile = GetTempFilePath();

        // Act
        await sorter.SortAsync(inputFile, outputFile, new LineDataComparer());

        // Assert
        var output = await File.ReadAllLinesAsync(outputFile);
        var expected = new[]
        {
            "1. Apple",
            "2. Banana",
            "3. Cherry",
            "4. Date"
        };
        Assert.That(output, Is.EqualTo(expected), $"Sorter: {sorterName}");
    }

    [TestCaseSource(nameof(GetSorters))]
    public async Task SortAsync_ReverseSorted_ReturnsCorrectOrder(ISorter sorter, string sorterName)
    {
        // Arrange
        var inputFile = CreateTempFile(
            "4. Zebra",
            "3. Yellow",
            "2. Xylophone",
            "1. Watermelon"
        );
        var outputFile = GetTempFilePath();

        // Act
        await sorter.SortAsync(inputFile, outputFile, new LineDataComparer());

        // Assert
        var output = await File.ReadAllLinesAsync(outputFile);
        var expected = new[]
        {
            "1. Watermelon",
            "2. Xylophone",
            "3. Yellow",
            "4. Zebra"
        };
        Assert.That(output, Is.EqualTo(expected), $"Sorter: {sorterName}");
    }

    // Test case source that provides all sorter implementations
    private static IEnumerable<TestCaseData> GetSorters()
    {
        var logger = new Mock<ILogger>().Object;
        
        yield return new TestCaseData(new ExternalMergeSorter(logger), "ExternalMergeSorter");
        yield return new TestCaseData(new StreamingSorter(logger), "StreamingSorter");
        yield return new TestCaseData(new ParallelExternalMergeSorter(logger), "ParallelExternalMergeSorter");
        yield return new TestCaseData(new ThreeDEyeSorterAdapter(), "ThreeDEyeSorter");
    }

    private string CreateTempFile(params string[] lines)
    {
        var tempFile = GetTempFilePath();
        File.WriteAllLines(tempFile, lines);
        return tempFile;
    }

    private string CreateEmptyTempFile()
    {
        var tempFile = GetTempFilePath();
        File.WriteAllText(tempFile, ""); // Creates a truly empty file
        return tempFile;
    }

    private string GetTempFilePath()
    {
        var tempFile = Path.GetTempFileName();
        _tempFiles.Add(tempFile);
        return tempFile;
    }

    public void Dispose()
    {
        // Clean up temporary files
        foreach (var tempFile in _tempFiles)
        {
            try
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        // Clean up temporary directories
        foreach (var tempDir in _tempDirectories)
        {
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
} 