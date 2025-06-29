using Microsoft.Extensions.Logging;
using Moq;
using _3dEYE.Generator.Algorithms;
using _3dEYE.Helpers;

namespace _3dEYE.Tests;

public class FileGeneratorTests
{
    [Test]
    public async Task FileGenerator_GenerateFileAsync_ShouldCreateFileWithSpecifiedSize()
    {
        // Arrange
        var logger = new Mock<ILogger>();
        var input = new[] { "Test string 1", "Test string 2", "Test string 3" };
        var fileGenerator = new FileGenerator(logger.Object, input);
        var expectedSize = 1024L; // 1KB

        // Act & Assert
        await FileHelper.WithCleanup(async tempFilePath =>
        {
            await fileGenerator.GenerateFileAsync(tempFilePath, expectedSize);

            // Assert
            Assert.That(File.Exists(tempFilePath), Is.True);
            var actualSize = new FileInfo(tempFilePath).Length;
            Assert.That(actualSize, Is.GreaterThanOrEqualTo(expectedSize));
        }, $"test_file_{Guid.NewGuid()}.txt", logger.Object);
    }
} 