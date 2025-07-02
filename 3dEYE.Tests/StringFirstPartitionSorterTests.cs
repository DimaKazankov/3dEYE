using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using _3dEYE.Sorter;
using _3dEYE.Sorter.Models;
using NUnit.Framework;

namespace _3dEYE.Tests;

[TestFixture]
public class StringFirstPartitionSorterTests : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _testDir;
    private readonly LineDataComparer _comparer;

    public StringFirstPartitionSorterTests()
    {
        _logger = NullLogger<StringFirstPartitionSorterTests>.Instance;
        _testDir = Path.Combine(Path.GetTempPath(), "StringFirstPartitionSorterTests");
        _comparer = new LineDataComparer();
        
        Directory.CreateDirectory(_testDir);
    }

    [Test]
    public async Task SortAsync_WithValidInput_ShouldProduceSortedOutput()
    {
        // Arrange
        var inputFile = Path.Combine(_testDir, "test_input.txt");
        var outputFile = Path.Combine(_testDir, "test_output.txt");
        
        // Create test data with mixed string prefixes
        var testData = new[]
        {
            "3. apple",
            "1. banana",
            "2. cherry",
            "5. date",
            "4. elderberry",
            "7. fig",
            "6. grape"
        };
        
        await File.WriteAllLinesAsync(inputFile, testData);
        
        var sorter = new StringFirstPartitionSorter(_logger, maxMemoryLines: 1000);

        // Act
        await sorter.SortAsync(inputFile, outputFile, _comparer);

        // Assert
        Assert.That(File.Exists(outputFile), Is.True);
        
        var sortedLines = await File.ReadAllLinesAsync(outputFile);
        Assert.That(sortedLines.Length, Is.EqualTo(7));
        
        // Verify sorting order: string part first (alphabetically), then number
        Assert.That(sortedLines[0], Is.EqualTo("1. banana"));
        Assert.That(sortedLines[1], Is.EqualTo("2. cherry"));
        Assert.That(sortedLines[2], Is.EqualTo("3. apple"));
        Assert.That(sortedLines[3], Is.EqualTo("4. elderberry"));
        Assert.That(sortedLines[4], Is.EqualTo("5. date"));
        Assert.That(sortedLines[5], Is.EqualTo("6. grape"));
        Assert.That(sortedLines[6], Is.EqualTo("7. fig"));
    }

    [Test]
    public async Task SortAsync_WithLargeDataset_ShouldHandleMemoryEfficiently()
    {
        // Arrange
        var inputFile = Path.Combine(_testDir, "large_input.txt");
        var outputFile = Path.Combine(_testDir, "large_output.txt");
        
        // Create a larger dataset with different string prefixes
        var words = new[] { "apple", "banana", "cherry", "date", "elderberry", "fig", "grape" };
        var testData = new List<string>();
        var random = new Random(42);
        
        for (var i = 1; i <= 10000; i++)
        {
            var word = words[random.Next(words.Length)];
            testData.Add($"{i}. {word}");
        }
        
        await File.WriteAllLinesAsync(inputFile, testData);
        
        var sorter = new StringFirstPartitionSorter(_logger, maxMemoryLines: 1000);

        // Act
        await sorter.SortAsync(inputFile, outputFile, _comparer);

        // Assert
        Assert.That(File.Exists(outputFile), Is.True);
        
        var sortedLines = await File.ReadAllLinesAsync(outputFile);
        Assert.That(sortedLines.Length, Is.EqualTo(10000));
        
        // Verify the first few lines are sorted correctly
        var firstLines = sortedLines.Take(10).ToArray();
        Assert.That(IsSorted(firstLines, _comparer), Is.True);
    }

    [Test]
    public async Task SortAsync_WithEmptyFile_ShouldCreateEmptyOutput()
    {
        // Arrange
        var inputFile = Path.Combine(_testDir, "empty_input.txt");
        var outputFile = Path.Combine(_testDir, "empty_output.txt");
        
        await File.WriteAllTextAsync(inputFile, "");
        
        var sorter = new StringFirstPartitionSorter(_logger);

        // Act
        await sorter.SortAsync(inputFile, outputFile, _comparer);

        // Assert
        Assert.That(File.Exists(outputFile), Is.True);
        var content = await File.ReadAllTextAsync(outputFile);
        Assert.That(content, Is.EqualTo(""));
    }

    [Test]
    public async Task SortAsync_WithSingleLine_ShouldPreserveLine()
    {
        // Arrange
        var inputFile = Path.Combine(_testDir, "single_input.txt");
        var outputFile = Path.Combine(_testDir, "single_output.txt");
        
        await File.WriteAllTextAsync(inputFile, "1. test");
        
        var sorter = new StringFirstPartitionSorter(_logger);

        // Act
        await sorter.SortAsync(inputFile, outputFile, _comparer);

        // Assert
        Assert.That(File.Exists(outputFile), Is.True);
        var content = await File.ReadAllTextAsync(outputFile);
        Assert.That(content.TrimEnd('\r', '\n'), Is.EqualTo("1. test"));
    }

    [Test]
    public async Task GetSortStatisticsAsync_WithValidFile_ShouldReturnStatistics()
    {
        // Arrange
        var inputFile = Path.Combine(_testDir, "stats_input.txt");
        var testData = new[] { "1. apple", "2. banana", "3. cherry" };
        await File.WriteAllLinesAsync(inputFile, testData);
        
        var sorter = new StringFirstPartitionSorter(_logger);

        // Act
        var statistics = await sorter.GetSortStatisticsAsync(inputFile, 1024 * 1024);

        // Assert
        Assert.That(statistics, Is.Not.Null);
        Assert.That(statistics.FileSizeBytes, Is.GreaterThan(0));
        Assert.That(statistics.BufferSizeBytes, Is.EqualTo(1024 * 1024));
        Assert.That(statistics.EstimatedChunks, Is.GreaterThan(0));
        Assert.That(statistics.EstimatedMergePasses, Is.EqualTo(1)); // Single merge pass for string-first partitioning
    }

    [Test]
    public void SortAsync_WithInvalidInputFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_testDir, "nonexistent.txt");
        var outputFile = Path.Combine(_testDir, "output.txt");
        
        var sorter = new StringFirstPartitionSorter(_logger);

        // Act & Assert
        var exception = Assert.ThrowsAsync<FileNotFoundException>(async () => 
            await sorter.SortAsync(nonExistentFile, outputFile, _comparer));
        Assert.That(exception, Is.Not.Null);
    }

    private bool IsSorted(string[] lines, IComparer<LineData> comparer)
    {
        for (var i = 0; i < lines.Length - 1; i++)
        {
            var line1 = LineData.FromString(lines[i], i);
            var line2 = LineData.FromString(lines[i + 1], i + 1);
            
            if (comparer.Compare(line1, line2) > 0)
                return false;
        }
        return true;
    }

    public void Dispose()
    {
        // Clean up test files
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, true);
        }
    }
} 