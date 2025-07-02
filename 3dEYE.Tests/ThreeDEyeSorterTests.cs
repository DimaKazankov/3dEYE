using _3dEYE.Sorter; 

namespace _3dEYE.Tests;

[TestFixture]
public class ThreeDEyeSorterTests : IDisposable
{
    private readonly ThreeDEyeSorter _sorter = new();
    private readonly List<string> _tempFiles = new();
    private readonly List<string> _tempDirectories = new();

    [Test]
    public async Task SortAsync_SingleLine_OutputsSameLine()
    {
        // Arrange
        var inputFile = CreateTempFile("1. Apple");
        var outputFile = GetTempFilePath();
        var tempDir = CreateTempDirectory();

        // Act
        await _sorter.SortAsync(inputFile, outputFile, tempDir);

        // Assert
        var output = await File.ReadAllTextAsync(outputFile);
        Assert.That(output.TrimEnd('\r', '\n'), Is.EqualTo("1. Apple"));
    }

    [Test]
    public async Task SortAsync_ExampleInput_SortsCorrectly()
    {
        // Arrange - Using the example from the requirements
        var inputFile = CreateTempFile(
            "415. Apple",
            "30432. Something something something",
            "1. Apple",
            "32. Cherry is the best",
            "2. Banana is yellow"
        );
        var outputFile = GetTempFilePath();
        var tempDir = CreateTempDirectory();

        // Act
        await _sorter.SortAsync(inputFile, outputFile, tempDir);

        // Assert - Expected output based on sorting criteria
        var output = await File.ReadAllLinesAsync(outputFile);
        var expected = new[]
        {
            "1. Apple",
            "415. Apple",
            "2. Banana is yellow",
            "32. Cherry is the best",
            "30432. Something something something"
        };
        Assert.That(output, Is.EqualTo(expected));
    }

    [Test]
    public async Task SortAsync_SameStringDifferentNumbers_SortsByNumber()
    {
        // Arrange
        var inputFile = CreateTempFile(
            "100. Same Text",
            "1. Same Text",
            "50. Same Text",
            "10. Same Text"
        );
        var outputFile = GetTempFilePath();
        var tempDir = CreateTempDirectory();

        // Act
        await _sorter.SortAsync(inputFile, outputFile, tempDir);

        // Assert
        var output = await File.ReadAllLinesAsync(outputFile);
        var expected = new[]
        {
            "1. Same Text",
            "10. Same Text",
            "50. Same Text",
            "100. Same Text"
        };
        Assert.That(output, Is.EqualTo(expected));
    }

    [Test]
    public async Task SortAsync_DifferentStrings_SortsAlphabetically()
    {
        // Arrange
        var inputFile = CreateTempFile(
            "1. Zebra",
            "2. Apple",
            "3. Banana",
            "4. Cherry"
        );
        var outputFile = GetTempFilePath();
        var tempDir = CreateTempDirectory();

        // Act
        await _sorter.SortAsync(inputFile, outputFile, tempDir);

        // Assert
        var output = await File.ReadAllLinesAsync(outputFile);
        var expected = new[]
        {
            "2. Apple",
            "3. Banana",
            "4. Cherry",
            "1. Zebra"
        };
        Assert.That(output, Is.EqualTo(expected));
    }

    [Test]
    public async Task SortAsync_LargeNumbers_HandlesCorrectly()
    {
        // Arrange
        var inputFile = CreateTempFile(
            "999999. Small text",
            "1. Small text",
            "1000000. Small text",
            "100. Small text"
        );
        var outputFile = GetTempFilePath();
        var tempDir = CreateTempDirectory();

        // Act
        await _sorter.SortAsync(inputFile, outputFile, tempDir);

        // Assert
        var output = await File.ReadAllLinesAsync(outputFile);
        var expected = new[]
        {
            "1. Small text",
            "100. Small text",
            "999999. Small text",
            "1000000. Small text"
        };
        Assert.That(output, Is.EqualTo(expected));
    }

    [Test]
    public async Task SortAsync_EmptyStringPart_HandlesCorrectly()
    {
        // Arrange
        var inputFile = CreateTempFile(
            "1. ",
            "2. Apple",
            "3. ",
            "4. Banana"
        );
        var outputFile = GetTempFilePath();
        var tempDir = CreateTempDirectory();

        // Act
        await _sorter.SortAsync(inputFile, outputFile, tempDir);

        // Assert
        var output = await File.ReadAllLinesAsync(outputFile);
        var expected = new[]
        {
            "1. ",
            "3. ",
            "2. Apple",
            "4. Banana"
        };
        Assert.That(output, Is.EqualTo(expected));
    }

    [Test]
    public async Task SortAsync_CaseSensitiveSorting_RespectsCase()
    {
        // Arrange
        var inputFile = CreateTempFile(
            "1. apple",
            "2. Apple",
            "3. APPLE",
            "4. aPPle"
        );
        var outputFile = GetTempFilePath();
        var tempDir = CreateTempDirectory();

        // Act
        await _sorter.SortAsync(inputFile, outputFile, tempDir);

        // Assert
        var output = await File.ReadAllLinesAsync(outputFile);
        var expected = new[]
        {
            "3. APPLE",
            "2. Apple",
            "4. aPPle",
            "1. apple"
        };
        Assert.That(output, Is.EqualTo(expected));
    }

    [Test]
    public async Task SortAsync_SpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var inputFile = CreateTempFile(
            "1. Apple!",
            "2. Apple",
            "3. Apple@",
            "4. Apple#"
        );
        var outputFile = GetTempFilePath();
        var tempDir = CreateTempDirectory();

        // Act
        await _sorter.SortAsync(inputFile, outputFile, tempDir);

        // Assert
        var output = await File.ReadAllLinesAsync(outputFile);
        var expected = new[]
        {
            "2. Apple",
            "4. Apple#",
            "1. Apple!",
            "3. Apple@"
        };
        Assert.That(output, Is.EqualTo(expected));
    }

    [Test]
    public async Task SortAsync_LargeFile_HandlesCorrectly()
    {
        // Arrange
        var lines = new List<string>();
        var random = new Random(42);
        
        // Generate 1000 lines with random numbers and predefined strings
        var strings = new[] { "Apple", "Banana", "Cherry", "Date", "Elderberry" };
        for (var i = 0; i < 1000; i++)
        {
            var number = random.Next(1, 10000);
            var text = strings[random.Next(strings.Length)];
            lines.Add($"{number}. {text}");
        }
        
        var inputFile = CreateTempFile(lines.ToArray());
        var outputFile = GetTempFilePath();
        var tempDir = CreateTempDirectory();

        // Act
        await _sorter.SortAsync(inputFile, outputFile, tempDir);

        // Assert
        var output = await File.ReadAllLinesAsync(outputFile);
        var expected = lines.OrderBy(line => 
        {
            var parts = line.Split(". ", 2);
            return (parts[1], int.Parse(parts[0]));
        }).ToArray();
        
        Assert.That(output, Is.EqualTo(expected));
    }

    [Test]
    public async Task SortAsync_UnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        var inputFile = CreateTempFile(
            "1. Apple",
            "2. Äpfel",
            "3. Apple",
            "4. 🍎 Apple"
        );
        var outputFile = GetTempFilePath();
        var tempDir = CreateTempDirectory();

        // Act
        await _sorter.SortAsync(inputFile, outputFile, tempDir);

        // Assert
        var output = await File.ReadAllLinesAsync(outputFile);
        var expected = new[]
        {
            "1. Apple",
            "3. Apple",
            "4. 🍎 Apple",
            "2. Äpfel"
        };
        Assert.That(output, Is.EqualTo(expected));
    }

    [Test]
    public async Task SortAsync_AlreadySorted_ReturnsSameOrder()
    {
        // Arrange
        var inputFile = CreateTempFile(
            "1. Apple",
            "2. Apple",
            "3. Banana",
            "4. Cherry"
        );
        var outputFile = GetTempFilePath();
        var tempDir = CreateTempDirectory();

        // Act
        await _sorter.SortAsync(inputFile, outputFile, tempDir);

        // Assert
        var output = await File.ReadAllLinesAsync(outputFile);
        var expected = new[]
        {
            "1. Apple",
            "2. Apple",
            "3. Banana",
            "4. Cherry"
        };
        Assert.That(output, Is.EqualTo(expected));
    }

    [Test]
    public async Task SortAsync_ReverseSorted_ReturnsCorrectOrder()
    {
        // Arrange
        var inputFile = CreateTempFile(
            "4. Cherry",
            "3. Banana",
            "2. Apple",
            "1. Apple"
        );
        var outputFile = GetTempFilePath();
        var tempDir = CreateTempDirectory();

        // Act
        await _sorter.SortAsync(inputFile, outputFile, tempDir);

        // Assert
        var output = await File.ReadAllLinesAsync(outputFile);
        var expected = new[]
        {
            "1. Apple",
            "2. Apple",
            "3. Banana",
            "4. Cherry"
        };
        Assert.That(output, Is.EqualTo(expected));
    }

    [Test]
    public async Task SortAsync_InputFileDoesNotExist_ThrowsException()
    {
        // Arrange
        var nonExistentFile = Path.GetTempFileName();
        File.Delete(nonExistentFile); // Ensure it doesn't exist
        var outputFile = GetTempFilePath();
        var tempDir = CreateTempDirectory();

        // Act & Assert
        var exception = Assert.ThrowsAsync<FileNotFoundException>(async () =>
            await _sorter.SortAsync(nonExistentFile, outputFile, tempDir));
    }

    [Test]
    public async Task SortAsync_InvalidFormatLines_HandlesTolerantly()
    {
        // Arrange - The sorter should be tolerant of malformed lines
        var inputFile = CreateTempFile(
            "1. Apple",
            "Invalid line without number",
            "2. Banana",
            "Another invalid line",
            "3. Cherry"
        );
        var outputFile = GetTempFilePath();
        var tempDir = CreateTempDirectory();

        // Act
        await _sorter.SortAsync(inputFile, outputFile, tempDir);

        // Assert - Should only process valid lines
        var output = await File.ReadAllLinesAsync(outputFile);
        var expected = new[]
        {
            "1. Apple",
            "2. Banana",
            "3. Cherry"
        };
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

    private string CreateTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ThreeDEyeSorterTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        _tempDirectories.Add(tempDir);
        return tempDir;
    }

    public void Dispose()
    {
        // Clean up temp files
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

        // Clean up temp directories
        foreach (var tempDir in _tempDirectories)
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
} 