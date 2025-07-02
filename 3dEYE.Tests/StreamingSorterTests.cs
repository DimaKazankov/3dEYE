using Microsoft.Extensions.Logging;
using _3dEYE.Sorter;
using _3dEYE.Sorter.Models;
using Moq;

namespace _3dEYE.Tests;

[TestFixture]
public class StreamingSorterTests : IDisposable
{
    private readonly ILogger<StreamingSorter> _logger;
    private readonly StreamingSorter _sorter;
    private readonly List<string> _tempFiles = new();

    public StreamingSorterTests()
    {
        _logger = new Mock<ILogger<StreamingSorter>>().Object;
        _sorter = new StreamingSorter(_logger, maxMemoryLines: 1000, bufferSize: 1024);
    }

    [Test]
    public async Task SortAsync_EmptyFile_CreatesEmptyOutput()
    {
        // Arrange
        var inputFile = CreateTempFile("");
        var outputFile = GetTempFilePath();

        // Act
        await _sorter.SortAsync(inputFile, outputFile, new LineDataComparer());

        // Assert
        Assert.That(File.Exists(outputFile), Is.True);
        Assert.That(new FileInfo(outputFile).Length, Is.EqualTo(0));
    }

    [Test]
    public async Task SortAsync_SingleLine_OutputsSameLine()
    {
        // Arrange
        var inputFile = CreateTempFile("hello world");
        var outputFile = GetTempFilePath();

        // Act
        await _sorter.SortAsync(inputFile, outputFile, new LineDataComparer());

        // Assert
        var output = await File.ReadAllTextAsync(outputFile);
        Assert.That(output, Is.EqualTo("hello world" + Environment.NewLine));
    }

    [Test]
    public async Task SortAsync_MultipleLines_SortsCorrectly()
    {
        // Arrange
        var inputFile = CreateTempFile(
            "zebra",
            "apple",
            "banana",
            "cherry"
        );
        var outputFile = GetTempFilePath();

        // Act
        await _sorter.SortAsync(inputFile, outputFile, new LineDataComparer());

        // Assert
        var output = await File.ReadAllLinesAsync(outputFile);
        Assert.That(output, Is.EqualTo(new[] { "apple", "banana", "cherry", "zebra" }));
    }

    [Test]
    public async Task SortAsync_LargeFile_HandlesMemoryLimit()
    {
        // Arrange
        var lines = Enumerable.Range(1000, 2000)
            .Select(i => $"line_{i:D6}")
            .ToList();
        
        // Shuffle the lines
        var random = new Random(42);
        lines = lines.OrderBy(x => random.Next()).ToList();
        
        var inputFile = CreateTempFile(lines.ToArray());
        var outputFile = GetTempFilePath();

        // Act
        await _sorter.SortAsync(inputFile, outputFile, new LineDataComparer());

        // Assert
        var output = await File.ReadAllLinesAsync(outputFile);
        var expected = lines.OrderBy(x => x).ToList();
        Assert.That(output, Is.EqualTo(expected));
    }

    [Test]
    public async Task SortAsync_DuplicateLines_HandlesCorrectly()
    {
        // Arrange
        var inputFile = CreateTempFile(
            "duplicate",
            "unique1",
            "duplicate",
            "unique2",
            "duplicate"
        );
        var outputFile = GetTempFilePath();

        // Act
        await _sorter.SortAsync(inputFile, outputFile, new LineDataComparer());

        // Assert
        var output = await File.ReadAllLinesAsync(outputFile);
        var expected = new[] { "duplicate", "duplicate", "duplicate", "unique1", "unique2" };
        Assert.That(output, Is.EqualTo(expected));
    }

    private string CreateTempFile(params string[] lines)
    {
        var tempFile = GetTempFilePath();
        File.WriteAllLines(tempFile, lines);
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
    }
} 