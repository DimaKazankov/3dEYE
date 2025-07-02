using _3dEYE.Sorter.Models;

namespace _3dEYE.Tests;

[TestFixture]
public class LineDataComparerTests
{
    private readonly LineDataComparer _comparer = new();

    [Test]
    public void Compare_WithValidLines_SortsCorrectly()
    {
        // Arrange
        var lines = new[]
        {
            LineData.FromString("415. Apple", 0),
            LineData.FromString("30432. Something something something", 1),
            LineData.FromString("1. Apple", 2),
            LineData.FromString("32. Cherry is the best", 3),
            LineData.FromString("2. Banana is yellow", 4)
        };

        // Act
        Array.Sort(lines, _comparer);

        // Assert
        Assert.That(lines[0].AsString(), Is.EqualTo("1. Apple"));
        Assert.That(lines[1].AsString(), Is.EqualTo("415. Apple"));
        Assert.That(lines[2].AsString(), Is.EqualTo("2. Banana is yellow"));
        Assert.That(lines[3].AsString(), Is.EqualTo("32. Cherry is the best"));
        Assert.That(lines[4].AsString(), Is.EqualTo("30432. Something something something"));
    }

    [Test]
    public void Compare_WithSameString_DifferentNumbers_SortsByNumber()
    {
        // Arrange
        var lines = new[]
        {
            LineData.FromString("415. Apple", 0),
            LineData.FromString("1. Apple", 1),
            LineData.FromString("100. Apple", 2)
        };

        // Act
        Array.Sort(lines, _comparer);

        // Assert
        Assert.That(lines[0].AsString(), Is.EqualTo("1. Apple"));
        Assert.That(lines[1].AsString(), Is.EqualTo("100. Apple"));
        Assert.That(lines[2].AsString(), Is.EqualTo("415. Apple"));
    }

    [Test]
    public void Compare_WithDifferentStrings_SortsAlphabetically()
    {
        // Arrange
        var lines = new[]
        {
            LineData.FromString("1. Zebra", 0),
            LineData.FromString("1. Apple", 1),
            LineData.FromString("1. Banana", 2)
        };

        // Act
        Array.Sort(lines, _comparer);

        // Assert
        Assert.That(lines[0].AsString(), Is.EqualTo("1. Apple"));
        Assert.That(lines[1].AsString(), Is.EqualTo("1. Banana"));
        Assert.That(lines[2].AsString(), Is.EqualTo("1. Zebra"));
    }

    [Test]
    public void Compare_WithEmptyLines_HandlesCorrectly()
    {
        // Arrange
        var lines = new[]
        {
            LineData.FromString("1. Apple", 0),
            LineData.FromString("", 1),
            LineData.FromString("2. Banana", 2),
            LineData.FromString("   ", 3)  // whitespace only
        };

        // Act
        Array.Sort(lines, _comparer);

        // Assert
        // Empty lines should be treated as strings with number 0, so they come first
        Assert.That(lines[0].AsString(), Is.EqualTo(""));
        Assert.That(lines[1].AsString(), Is.EqualTo("   "));
        Assert.That(lines[2].AsString(), Is.EqualTo("1. Apple"));
        Assert.That(lines[3].AsString(), Is.EqualTo("2. Banana"));
    }
} 