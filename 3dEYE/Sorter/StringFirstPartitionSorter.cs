using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Logging;
using _3dEYE.Sorter.Models;

namespace _3dEYE.Sorter;

public class StringFirstPartitionSorter(
    ILogger logger,
    int maxMemoryLines = 100000,
    int bufferSize = 1024 * 1024,
    int maxDegreeOfParallelism = 0)
    : ISorter
{
    private readonly int _maxDegreeOfParallelism = maxDegreeOfParallelism > 0 
        ? maxDegreeOfParallelism 
        : Environment.ProcessorCount;

    public async Task SortAsync(
        string inputFilePath, 
        string outputFilePath, 
        IComparer<LineData> comparer, 
        CancellationToken cancellationToken = default)
    {
        await SortAsync(inputFilePath, outputFilePath, bufferSize, comparer, cancellationToken).ConfigureAwait(false);
    }

    public async Task SortAsync(
        string inputFilePath, 
        string outputFilePath, 
        int bufferSizeBytes, 
        IComparer<LineData> comparer, 
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException($"Input file not found: {inputFilePath}");

        var fileInfo = new FileInfo(inputFilePath);
        logger.LogInformation("Starting string-first partition sort for file: {FilePath} ({Size} bytes) with {ThreadCount} threads", 
            inputFilePath, fileInfo.Length, _maxDegreeOfParallelism);

        var startTime = DateTime.UtcNow;

        try
        {
            await PerformStringFirstPartitionSortAsync(
                inputFilePath, 
                outputFilePath, 
                bufferSizeBytes, 
                comparer, 
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during string-first partition sort");
            throw;
        }

        var totalTime = DateTime.UtcNow - startTime;
        var outputInfo = new FileInfo(outputFilePath);
        logger.LogInformation("String-first partition sort completed in {TotalTime:g}. Output file: {OutputPath} ({Size} bytes)", 
            totalTime, outputFilePath, outputInfo.Length);
    }

    private async Task PerformStringFirstPartitionSortAsync(
        string inputFilePath,
        string outputFilePath,
        int bufferSizeBytes,
        IComparer<LineData> comparer,
        CancellationToken cancellationToken)
    {
        // Create temporary directory for partition files
        var tempDir = Path.Combine(Path.GetTempPath(), $"string_partition_sort_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Phase 1: Analyze file to understand string distribution
            logger.LogInformation("Phase 1: Analyzing string distribution");
            var prefixAnalysis = await AnalyzeStringDistributionAsync(
                inputFilePath, bufferSizeBytes, cancellationToken).ConfigureAwait(false);

            // Phase 2: Partition file by string prefixes
            logger.LogInformation("Phase 2: Partitioning by string prefixes");
            var partitionFiles = await PartitionByStringPrefixesAsync(
                inputFilePath, tempDir, prefixAnalysis, bufferSizeBytes, cancellationToken).ConfigureAwait(false);

            // Phase 3: Sort each partition and merge to output
            logger.LogInformation("Phase 3: Sorting partitions and writing output");
            await SortPartitionsAndMergeAsync(
                partitionFiles, outputFilePath, comparer, bufferSizeBytes, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // Clean up temporary directory
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch (Exception cleanupEx)
            {
                logger.LogWarning("Failed to clean up temporary directory {TempDir}: {Message}", tempDir, cleanupEx.Message);
            }
        }
    }

    private async Task<PrefixAnalysis> AnalyzeStringDistributionAsync(
        string inputFilePath,
        int bufferSizeBytes,
        CancellationToken cancellationToken)
    {
        var prefixCounts = new ConcurrentDictionary<string, long>();
        var totalLines = 0L;
        var maxPrefixLength = 3; // Start with 3-character prefixes

        using var reader = new StreamReader(inputFilePath, Encoding.UTF8, true, bufferSizeBytes);
        var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

        while (line != null && !cancellationToken.IsCancellationRequested)
        {
            totalLines++;
            var prefix = ExtractStringPrefix(line, maxPrefixLength);
            prefixCounts.AddOrUpdate(prefix, 1, (_, count) => count + 1);

            line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        }

        // Calculate optimal prefix length based on distribution
        var optimalPrefixLength = CalculateOptimalPrefixLength(prefixCounts, totalLines, maxMemoryLines);
        
        // Rebuild prefix counts with optimal length
        var optimalPrefixCounts = new ConcurrentDictionary<string, long>();
        using var reader2 = new StreamReader(inputFilePath, Encoding.UTF8, true, bufferSizeBytes);
        line = await reader2.ReadLineAsync(cancellationToken).ConfigureAwait(false);

        while (line != null && !cancellationToken.IsCancellationRequested)
        {
            var prefix = ExtractStringPrefix(line, optimalPrefixLength);
            optimalPrefixCounts.AddOrUpdate(prefix, 1, (_, count) => count + 1);

            line = await reader2.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("Analysis complete: {TotalLines} lines, {PrefixCount} prefixes, optimal length: {PrefixLength}", 
            totalLines, optimalPrefixCounts.Count, optimalPrefixLength);

        return new PrefixAnalysis
        {
            TotalLines = totalLines,
            PrefixCounts = optimalPrefixCounts.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            OptimalPrefixLength = optimalPrefixLength
        };
    }

    private async Task<List<string>> PartitionByStringPrefixesAsync(
        string inputFilePath,
        string tempDir,
        PrefixAnalysis analysis,
        int bufferSizeBytes,
        CancellationToken cancellationToken)
    {
        var partitionFiles = new ConcurrentDictionary<string, string>();
        var partitionWriters = new ConcurrentDictionary<string, StreamWriter>();

        // Create partition files and writers
        foreach (var prefix in analysis.PrefixCounts.Keys)
        {
            var partitionFile = Path.Combine(tempDir, $"partition_{prefix.ReplaceInvalidChars()}.txt");
            partitionFiles[prefix] = partitionFile;
            partitionWriters[prefix] = new StreamWriter(partitionFile, false, Encoding.UTF8, bufferSizeBytes);
        }

        try
        {
            // Stream through input file and route lines to appropriate partitions
            using var reader = new StreamReader(inputFilePath, Encoding.UTF8, true, bufferSizeBytes);
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            var linesProcessed = 0L;

            while (line != null && !cancellationToken.IsCancellationRequested)
            {
                var prefix = ExtractStringPrefix(line, analysis.OptimalPrefixLength);
                
                if (partitionWriters.TryGetValue(prefix, out var writer))
                {
                    await writer.WriteLineAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
                }

                linesProcessed++;
                if (linesProcessed % 1000000 == 0)
                {
                    logger.LogDebug("Processed {LinesProcessed} lines", linesProcessed);
                }

                line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            }

            logger.LogInformation("Partitioning complete: {LinesProcessed} lines distributed to {PartitionCount} partitions", 
                linesProcessed, partitionFiles.Count);
        }
        finally
        {
            // Close all partition writers
            foreach (var writer in partitionWriters.Values)
            {
                await writer.DisposeAsync().ConfigureAwait(false);
            }
        }

        return partitionFiles.Values.ToList();
    }

    private async Task SortPartitionsAndMergeAsync(
        List<string> partitionFiles,
        string outputFilePath,
        IComparer<LineData> comparer,
        int bufferSizeBytes,
        CancellationToken cancellationToken)
    {
        // Sort partitions in parallel and write to output in order
        var sortedPartitions = new ConcurrentDictionary<string, List<LineData>>();
        
        // Process partitions in parallel
        var tasks = partitionFiles.Select(async partitionFile =>
        {
            var prefix = ExtractPrefixFromFilename(partitionFile);
            var sortedLines = await SortPartitionAsync(partitionFile, comparer, bufferSizeBytes, cancellationToken).ConfigureAwait(false);
            sortedPartitions[prefix] = sortedLines;
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);

        // Write sorted partitions to output in alphabetical order
        await using var outputWriter = new StreamWriter(outputFilePath, false, Encoding.UTF8, bufferSizeBytes);
        
        var sortedPrefixes = sortedPartitions.Keys.OrderBy(p => p).ToList();
        
        foreach (var prefix in sortedPrefixes)
        {
            if (sortedPartitions.TryGetValue(prefix, out var lines))
            {
                foreach (var lineData in lines)
                {
                    await outputWriter.WriteLineAsync(lineData.Content, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        logger.LogInformation("Sorting and merging complete: {PartitionCount} partitions merged to output", sortedPartitions.Count);
    }

    private async Task<List<LineData>> SortPartitionAsync(
        string partitionFile,
        IComparer<LineData> comparer,
        int bufferSizeBytes,
        CancellationToken cancellationToken)
    {
        var lines = new List<LineData>();
        var currentPosition = 0L;

        using var reader = new StreamReader(partitionFile, Encoding.UTF8, true, bufferSizeBytes);
        var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

        while (line != null && !cancellationToken.IsCancellationRequested)
        {
            var lineData = LineData.FromString(line, currentPosition);
            lines.Add(lineData);
            currentPosition = reader.BaseStream.Position;

            line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
        }

        // Sort the partition in memory
        lines.Sort(comparer);
        
        logger.LogDebug("Sorted partition {PartitionFile}: {LineCount} lines", 
            Path.GetFileName(partitionFile), lines.Count);

        return lines;
    }

    private static string ExtractStringPrefix(string line, int maxLength)
    {
        // Extract the string part (after ". ") and get its prefix
        var separatorIndex = line.IndexOf(". ", StringComparison.Ordinal);
        if (separatorIndex == -1)
        {
            // If no separator, use the whole line as prefix
            return line.Length <= maxLength ? line : line[..maxLength];
        }

        var stringPart = line[(separatorIndex + 2)..]; // Skip ". "
        return stringPart.Length <= maxLength ? stringPart : stringPart[..maxLength];
    }

    private static string ExtractPrefixFromFilename(string filename)
    {
        // Extract prefix from filename like "partition_ABC.txt"
        var fileName = Path.GetFileNameWithoutExtension(filename);
        return fileName.Replace("partition_", "");
    }

    private static int CalculateOptimalPrefixLength(
        ConcurrentDictionary<string, long> prefixCounts, 
        long totalLines, 
        int maxMemoryLines)
    {
        // Calculate optimal prefix length to ensure partitions fit in memory
        var currentLength = 1;
        var maxLength = 5; // Maximum prefix length to consider

        for (var length = 1; length <= maxLength; length++)
        {
            var estimatedPartitions = Math.Pow(26, length); // Assume 26 characters
            var avgLinesPerPartition = totalLines / estimatedPartitions;

            if (avgLinesPerPartition <= maxMemoryLines)
            {
                currentLength = length;
                break;
            }
        }

        return currentLength;
    }

    public Task<SortStatistics> GetSortStatisticsAsync(
        string inputFilePath, 
        int bufferSizeBytes)
    {
        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException($"Input file not found: {inputFilePath}");

        var fileInfo = new FileInfo(inputFilePath);
        
        // Estimate based on typical string distribution
        var estimatedPrefixes = 100; // Conservative estimate
        var estimatedLinesPerPartition = fileInfo.Length / (estimatedPrefixes * 200); // Assume 200 bytes per line
        
        var statistics = new SortStatistics
        {
            FileSizeBytes = fileInfo.Length,
            BufferSizeBytes = bufferSizeBytes,
            EstimatedChunks = estimatedPrefixes,
            EstimatedMergePasses = 1, // Single merge pass
            EstimatedTotalIOPerFile = 3 // Read + partition + merge
        };

        return Task.FromResult(statistics);
    }

    private class PrefixAnalysis
    {
        public long TotalLines { get; set; }
        public Dictionary<string, long> PrefixCounts { get; set; } = new();
        public int OptimalPrefixLength { get; set; }
    }
}

public static class StringExtensions
{
    public static string ReplaceInvalidChars(this string str)
    {
        return str.Replace('/', '_').Replace('\\', '_').Replace(':', '_')
                 .Replace('*', '_').Replace('?', '_').Replace('"', '_')
                 .Replace('<', '_').Replace('>', '_').Replace('|', '_');
    }
} 