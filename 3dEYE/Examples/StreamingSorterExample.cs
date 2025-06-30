using Microsoft.Extensions.Logging;
using _3dEYE.Sorter;
using _3dEYE.Sorter.Models;
using Serilog;

namespace _3dEYE.Examples;

public static class StreamingSorterExample
{
    public static async Task RunExampleAsync()
    {
        // Create a logger using Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .MinimumLevel.Information()
            .CreateLogger();
        
        var loggerFactory = LoggerFactory.Create(builder => 
            builder.AddSerilog(dispose: true));
        var logger = loggerFactory.CreateLogger<StreamingSorter>();

        // Create streaming sorter with custom memory limit
        var sorter = new StreamingSorter(
            logger: logger,
            maxMemoryLines: 50000, // Keep max 50,000 lines in memory
            bufferSize: 1024 * 1024 // 1MB I/O buffer
        );

        // Create a sample input file with unsorted data
        var inputFile = "streaming_input.txt";
        var outputFile = "streaming_output.txt";

        try
        {
            // Generate sample data
            await GenerateSampleDataAsync(inputFile);

            Console.WriteLine($"Input file created: {inputFile}");
            Console.WriteLine($"File size: {new FileInfo(inputFile).Length:N0} bytes");

            // Get sorting statistics before sorting
            var stats = await sorter.GetSortStatisticsAsync(inputFile, 1024 * 1024);
            Console.WriteLine($"Estimated chunks: {stats.EstimatedChunks}");
            Console.WriteLine($"Estimated merge passes: {stats.EstimatedMergePasses}");
            Console.WriteLine($"Estimated I/O operations per file: {stats.EstimatedTotalIOPerFile}");

            // Perform streaming sort
            Console.WriteLine("\nStarting streaming sort...");
            var startTime = DateTime.UtcNow;

            await sorter.SortAsync(
                inputFilePath: inputFile,
                outputFilePath: outputFile,
                comparer: new LineDataComparer()
            );

            var duration = DateTime.UtcNow - startTime;
            Console.WriteLine($"Sorting completed in {duration:g}");
            Console.WriteLine($"Output file: {outputFile}");
            Console.WriteLine($"Output size: {new FileInfo(outputFile).Length:N0} bytes");

            // Verify the result
            await VerifySortingResultAsync(outputFile);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            // Cleanup
            CleanupFiles(inputFile, outputFile);
        }
    }

    private static async Task GenerateSampleDataAsync(string filePath)
    {
        var random = new Random(42); // Fixed seed for reproducible results
        var words = new[] { "apple", "banana", "cherry", "date", "elderberry", "fig", "grape", "honeydew" };
        
        await using var writer = new StreamWriter(filePath);
        
        // Generate 100,000 lines of random data
        for (int i = 0; i < 100000; i++)
        {
            var word1 = words[random.Next(words.Length)];
            var word2 = words[random.Next(words.Length)];
            var number = random.Next(1000, 9999);
            await writer.WriteLineAsync($"{word1}_{word2}_{number:D4}");
        }
    }

    private static async Task VerifySortingResultAsync(string filePath)
    {
        Console.WriteLine("\nVerifying sorting result...");
        
        var lines = await File.ReadAllLinesAsync(filePath);
        var isSorted = true;
        
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.Compare(lines[i - 1], lines[i], StringComparison.Ordinal) > 0)
            {
                isSorted = false;
                Console.WriteLine($"Sorting error at line {i}: '{lines[i - 1]}' > '{lines[i]}'");
                break;
            }
        }
        
        if (isSorted)
        {
            Console.WriteLine("✓ File is correctly sorted!");
        }
        else
        {
            Console.WriteLine("✗ File is not correctly sorted!");
        }
        
        Console.WriteLine($"Total lines: {lines.Length:N0}");
    }

    private static void CleanupFiles(params string[] files)
    {
        foreach (var file in files)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                    Console.WriteLine($"Cleaned up: {file}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to clean up {file}: {ex.Message}");
            }
        }
    }
} 