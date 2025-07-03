using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Moq;
using _3dEYE.Generator;
using _3dEYE.Generator.Algorithms;
using _3dEYE.Helpers;

namespace _3dEYE.Tests;

[TestFixture]
public class FileGeneratorsTests
{
    private Mock<ILogger> _logger;
    private static readonly string[] Strings = ["Apple", "Banana is yellow", "Cherry is the best", "Something something something"];

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger>();
    }

    private static IEnumerable<TestCaseData> FileGeneratorTestCases()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        var logger = loggerFactory.CreateLogger<FileGeneratorsTests>();
        yield return new TestCaseData(new FileGenerator(logger, Strings), 
            "Basic FileGenerator").SetName("Basic_FileGenerator");
        yield return new TestCaseData(new OptimizedFileGenerator(logger, Strings),
            "Optimized FileGenerator (Default)").SetName("Optimized_FileGenerator_Default");
        yield return new TestCaseData(
            new OptimizedFileGenerator(logger, Strings, bufferSize: 4 * 1024 * 1024, batchSize: 2000),
            "Optimized FileGenerator (Large Buffer)").SetName("Optimized_FileGenerator_LargeBuffer");
        yield return new TestCaseData(
            new ParallelFileGenerator(logger, Strings, Path.Combine(Path.GetTempPath(), "parallel_test.txt")),
            "Parallel FileGenerator (Default)").SetName("Parallel_FileGenerator_Default");
        yield return new TestCaseData(
            new ParallelFileGenerator(logger, Strings, 
                Path.Combine(Path.GetTempPath(), "parallel_test.txt"), chunkSize: 50 * 1024 * 1024, maxDegreeOfParallelism: 4),
            "Parallel FileGenerator (Large Chunks)").SetName("Parallel_FileGenerator_LargeChunks");
        yield return new TestCaseData(
            new ThreeDEyeFilesGenerator(logger, Strings),
            "ThreeDEyeFilesGenerator (Default)").SetName("ThreeDEyeFilesGenerator_Default");
        yield return new TestCaseData(
            new ThreeDEyeFilesGenerator(logger, Strings, chunkSize: 50 * 1024 * 1024, maxDegreeOfParallelism: 4),
            "ThreeDEyeFilesGenerator (Large Chunks)").SetName("ThreeDEyeFilesGenerator_LargeChunks");
    }

    [Test]
    [TestCaseSource(nameof(FileGeneratorTestCases))]
    public async Task FileGenerator_GenerateFileAsync_ShouldCreateFileWithCorrectFormat(IFileGenerator fileGenerator, string generatorName)
    {
        var expectedSize = 2048L;
        await FileHelper.WithCleanup(async tempFilePath =>
        {
            await fileGenerator.GenerateFileAsync(tempFilePath, expectedSize);
            Assert.That(File.Exists(tempFilePath), Is.True, $"{generatorName}: File should exist");
            var lines = await File.ReadAllLinesAsync(tempFilePath);
            Assert.That(lines.Length, Is.GreaterThan(0), $"{generatorName}: File should contain at least one line");
            foreach (var line in lines)
            {
                Assert.That(line, Is.Not.Empty, $"{generatorName}: Line should not be empty");
                var parts = line.Split(". ", 2);
                Assert.That(parts.Length, Is.EqualTo(2), $"{generatorName}: Line should contain exactly one '. ' separator: '{line}'");
                Assert.That(long.TryParse(parts[0], out _), Is.True, $"{generatorName}: First part should be a number: '{parts[0]}'");
                Assert.That(parts[1], Is.Not.Empty, $"{generatorName}: String part should not be empty");
                Assert.That(Strings, Does.Contain(parts[1]),
                    $"{generatorName}: String part '{parts[1]}' should be exactly one of the input words");
            }
        }, $"format_test_{Guid.NewGuid()}.txt", _logger.Object);
    }
} 