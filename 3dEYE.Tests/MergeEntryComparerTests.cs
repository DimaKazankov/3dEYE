using _3dEYE.Sorter.Models;

namespace _3dEYE.Tests;

[TestFixture]
public class MergeEntryComparerTests
{
    private readonly MergeEntryComparer _comparer = MergeEntryComparer.Instance;

    [Test]
    public void Compare_ExampleInput_SortsCorrectly()
    {
        var entries = new[]
        {
            CreateMergeEntry("415. Apple", 415),
            CreateMergeEntry("30432. Something something something", 30432),
            CreateMergeEntry("1. Apple", 1),
            CreateMergeEntry("32. Cherry is the best", 32),
            CreateMergeEntry("2. Banana is yellow", 2)
        };
        var sorted = entries.OrderBy(x => x, _comparer).ToArray();
        var expected = new[]
        {
            CreateMergeEntry("1. Apple", 1),
            CreateMergeEntry("415. Apple", 415),
            CreateMergeEntry("2. Banana is yellow", 2),
            CreateMergeEntry("32. Cherry is the best", 32),
            CreateMergeEntry("30432. Something something something", 30432)
        };
        Assert.That(sorted, Is.EqualTo(expected));
    }

    [Test]
    public void Compare_SameStringDifferentNumbers_SortsByNumber()
    {
        // Arrange
        var entries = new[]
        {
            CreateMergeEntry("100. Same Text", 100),
            CreateMergeEntry("1. Same Text", 1),
            CreateMergeEntry("50. Same Text", 50),
            CreateMergeEntry("10. Same Text", 10)
        };

        // Act
        var sorted = entries.OrderBy(x => x, _comparer).ToArray();

        // Assert
        var expected = new[]
        {
            CreateMergeEntry("1. Same Text", 1),
            CreateMergeEntry("10. Same Text", 10),
            CreateMergeEntry("50. Same Text", 50),
            CreateMergeEntry("100. Same Text", 100)
        };

        Assert.That(sorted, Is.EqualTo(expected));
    }

    [Test]
    public void Compare_DifferentStrings_SortsAlphabetically()
    {
        // Arrange
        var entries = new[]
        {
            CreateMergeEntry("1. Zebra", 1),
            CreateMergeEntry("2. Apple", 2),
            CreateMergeEntry("3. Banana", 3),
            CreateMergeEntry("4. Cherry", 4)
        };

        // Act
        var sorted = entries.OrderBy(x => x, _comparer).ToArray();

        // Assert
        var expected = new[]
        {
            CreateMergeEntry("2. Apple", 2),
            CreateMergeEntry("3. Banana", 3),
            CreateMergeEntry("4. Cherry", 4),
            CreateMergeEntry("1. Zebra", 1)
        };

        Assert.That(sorted, Is.EqualTo(expected));
    }

    [Test]
    public void Compare_LargeNumbers_HandlesCorrectly()
    {
        // Arrange
        var entries = new[]
        {
            CreateMergeEntry("999999. Small text", 999999),
            CreateMergeEntry("1. Small text", 1),
            CreateMergeEntry("1000000. Small text", 1000000),
            CreateMergeEntry("100. Small text", 100)
        };

        // Act
        var sorted = entries.OrderBy(x => x, _comparer).ToArray();

        // Assert
        var expected = new[]
        {
            CreateMergeEntry("1. Small text", 1),
            CreateMergeEntry("100. Small text", 100),
            CreateMergeEntry("999999. Small text", 999999),
            CreateMergeEntry("1000000. Small text", 1000000)
        };

        Assert.That(sorted, Is.EqualTo(expected));
    }

    [Test]
    public void Compare_EmptyStringPart_HandlesCorrectly()
    {
        // Arrange
        var entries = new[]
        {
            CreateMergeEntry("1. ", 1),
            CreateMergeEntry("2. Apple", 2),
            CreateMergeEntry("3. ", 3),
            CreateMergeEntry("4. Banana", 4)
        };

        // Act
        var sorted = entries.OrderBy(x => x, _comparer).ToArray();

        // Assert
        var expected = new[]
        {
            CreateMergeEntry("1. ", 1),
            CreateMergeEntry("3. ", 3),
            CreateMergeEntry("2. Apple", 2),
            CreateMergeEntry("4. Banana", 4)
        };

        Assert.That(sorted, Is.EqualTo(expected));
    }

    [Test]
    public void Compare_CaseSensitiveSorting_RespectsCase()
    {
        // Arrange
        var entries = new[]
        {
            CreateMergeEntry("1. apple", 1),
            CreateMergeEntry("2. Apple", 2),
            CreateMergeEntry("3. APPLE", 3),
            CreateMergeEntry("4. aPPle", 4)
        };

        // Act
        var sorted = entries.OrderBy(x => x, _comparer).ToArray();

        // Assert
        var expected = new[]
        {
            CreateMergeEntry("3. APPLE", 3),
            CreateMergeEntry("2. Apple", 2),
            CreateMergeEntry("4. aPPle", 4),
            CreateMergeEntry("1. apple", 1)
        };

        Assert.That(sorted, Is.EqualTo(expected));
    }

    [Test]
    public void Compare_AlreadySorted_ReturnsSameOrder()
    {
        // Arrange
        var entries = new[]
        {
            CreateMergeEntry("1. Apple", 1),
            CreateMergeEntry("2. Banana", 2),
            CreateMergeEntry("3. Cherry", 3),
            CreateMergeEntry("4. Date", 4)
        };

        // Act
        var sorted = entries.OrderBy(x => x, _comparer).ToArray();

        // Assert
        var expected = new[]
        {
            CreateMergeEntry("1. Apple", 1),
            CreateMergeEntry("2. Banana", 2),
            CreateMergeEntry("3. Cherry", 3),
            CreateMergeEntry("4. Date", 4)
        };

        Assert.That(sorted, Is.EqualTo(expected));
    }

    [Test]
    public void Compare_ReverseSorted_ReturnsCorrectOrder()
    {
        // Arrange
        var entries = new[]
        {
            CreateMergeEntry("4. Zebra", 4),
            CreateMergeEntry("3. Yellow", 3),
            CreateMergeEntry("2. Xylophone", 2),
            CreateMergeEntry("1. Watermelon", 1)
        };

        // Act
        var sorted = entries.OrderBy(x => x, _comparer).ToArray();

        // Assert
        var expected = new[]
        {
            CreateMergeEntry("1. Watermelon", 1),
            CreateMergeEntry("2. Xylophone", 2),
            CreateMergeEntry("3. Yellow", 3),
            CreateMergeEntry("4. Zebra", 4)
        };

        Assert.That(sorted, Is.EqualTo(expected));
    }

    [Test]
    public void Compare_DirectComparisons_WorkCorrectly()
    {
        // Test direct comparison method calls
        var entry1 = CreateMergeEntry("1. Apple", 1);
        var entry2 = CreateMergeEntry("2. Apple", 2);
        var entry3 = CreateMergeEntry("1. Banana", 1);
        var entry4 = CreateMergeEntry("1. Apple", 1);

        // Same key, different numbers
        Assert.That(_comparer.Compare(entry1, entry2), Is.LessThan(0)); // 1 < 2
        Assert.That(_comparer.Compare(entry2, entry1), Is.GreaterThan(0)); // 2 > 1

        // Different keys, same number
        Assert.That(_comparer.Compare(entry1, entry3), Is.LessThan(0)); // Apple < Banana
        Assert.That(_comparer.Compare(entry3, entry1), Is.GreaterThan(0)); // Banana > Apple

        // Same key, same number
        Assert.That(_comparer.Compare(entry1, entry4), Is.EqualTo(0)); // Equal
        Assert.That(_comparer.Compare(entry4, entry1), Is.EqualTo(0)); // Equal
    }

    /// <summary>
    /// Helper method to create a MergeEntry with the given line and number
    /// </summary>
    private static MergeEntry CreateMergeEntry(string line, int number)
    {
        var dot = line.IndexOf('.');
        if (dot < 0 || dot + 1 >= line.Length || line[dot + 1] != ' ')
        {
            throw new ArgumentException($"Invalid line format: {line}");
        }

        var keyStart = dot + 2; // Skip ". "
        var keyLen = line.Length - keyStart;

        return new MergeEntry(line, keyStart, keyLen, number, 0);
    }
} 