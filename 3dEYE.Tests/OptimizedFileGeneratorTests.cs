using Microsoft.Extensions.Logging;
using Moq;
using _3dEYE.Generator.Algorithms;
using _3dEYE.Helpers;

namespace _3dEYE.Tests;

public class OptimizedFileGeneratorTests
{
    [Test]
    public async Task OptimizedFileGenerator_WithLargeBuffer_ShouldWorkCorrectly()
    {
        // Arrange
        var logger = new Mock<ILogger>();
        var input = new[] { "Large buffer test" };
        var largeBufferSize = 4 * 1024 * 1024; // 4MB buffer
        var optimizedGenerator = new OptimizedFileGenerator(logger.Object, input, bufferSize: largeBufferSize, batchSize: 2000);
        var expectedSize = 2048L; // 2KB

        // Act & Assert
        await FileHelper.WithCleanup(async tempFilePath =>
        {
            await optimizedGenerator.GenerateFileAsync(tempFilePath, expectedSize);

            // Assert
            Assert.That(File.Exists(tempFilePath), Is.True);
            var actualSize = new FileInfo(tempFilePath).Length;
            Assert.That(actualSize, Is.GreaterThanOrEqualTo(expectedSize));
        }, $"large_buffer_test_{Guid.NewGuid()}.txt", logger.Object);
    }
} 