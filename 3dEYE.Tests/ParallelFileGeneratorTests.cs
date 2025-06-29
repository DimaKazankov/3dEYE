using Microsoft.Extensions.Logging;
using Moq;
using _3dEYE.Generator.Algorithms;
using _3dEYE.Helpers;

namespace _3dEYE.Tests;

public class ParallelFileGeneratorTests
{
    [Test]
    public async Task ParallelFileGenerator_WithLargeChunks_ShouldWorkCorrectly()
    {
        // Arrange
        var logger = new Mock<ILogger>();
        var input = new[] { "Large chunk test" };
        var largeChunkSize = 50 * 1024 * 1024; // 50MB chunks
        var parallelGenerator = new ParallelFileGenerator(logger.Object, input, chunkSize: largeChunkSize, maxDegreeOfParallelism: 4);
        var expectedSize = 1024 * 1024L; // 1MB

        // Act & Assert
        await FileHelper.WithCleanup(async tempFilePath =>
        {
            await parallelGenerator.GenerateFileAsync(tempFilePath, expectedSize);

            // Assert
            Assert.That(File.Exists(tempFilePath), Is.True);
            var actualSize = new FileInfo(tempFilePath).Length;
            Assert.That(actualSize, Is.GreaterThanOrEqualTo(expectedSize));
        }, $"large_chunk_test_{Guid.NewGuid()}.txt", logger.Object);
    }
} 