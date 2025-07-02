using System.Text;
using _3dEYE.Sorter.Models;

namespace _3dEYE.Sorter.Utils;

public class MergeManager(int bufferSize = 1024 * 1024, IComparer<LineData>? comparer = null)
{
    private readonly IComparer<LineData> _comparer = comparer ?? new LineDataComparer();

    public async Task MergeChunksAsync(
        List<string> chunkFiles, 
        string outputFilePath, 
        CancellationToken cancellationToken = default)
    {
        if (chunkFiles.Count == 0)
            throw new ArgumentException("No chunk files to merge", nameof(chunkFiles));

        if (chunkFiles.Count == 1)
        {
            // Single chunk, just copy to output
            File.Copy(chunkFiles[0], outputFilePath, true);
            return;
        }

        // Use multi-pass merge for large numbers of chunks
        var currentChunks = new List<string>(chunkFiles);
        var passNumber = 0;
        var tempDirectory = Path.GetDirectoryName(chunkFiles[0]) ?? Path.GetTempPath();

        while (currentChunks.Count > 1)
        {
            var nextChunks = new List<string>();
            var batchSize = Math.Min(10, currentChunks.Count); // Merge up to 10 chunks at once

            for (var i = 0; i < currentChunks.Count; i += batchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = currentChunks.Skip(i).Take(batchSize).ToList();
                var mergedFile = await MergeBatchAsync(batch, passNumber, tempDirectory, cancellationToken).ConfigureAwait(false);
                nextChunks.Add(mergedFile);
            }

            // Clean up intermediate files from previous pass
            if (passNumber > 0)
            {
                CleanupIntermediateFiles(currentChunks);
            }

            currentChunks = nextChunks;
            passNumber++;
        }

        // Move final result to output location
        if (currentChunks.Count == 1)
        {
            File.Move(currentChunks[0], outputFilePath, true);
        }
    }
    
    private async Task<string> MergeBatchAsync(
        List<string> chunkFiles, 
        int passNumber, 
        string tempDirectory,
        CancellationToken cancellationToken)
    {
        var outputFile = Path.Combine(
            tempDirectory, 
            $"merge_pass_{passNumber}_{Guid.NewGuid():N}.tmp");

        await using var outputWriter = new StreamWriter(outputFile, false, Encoding.UTF8, bufferSize);
        
        // Create readers for all chunk files
        var readers = new List<StreamReader>();
        var currentLines = new List<LineData?>();
        
        try
        {
            // Initialize readers and read first line from each
            foreach (var chunkFile in chunkFiles)
            {
                var reader = new StreamReader(chunkFile, Encoding.UTF8, true, bufferSize);
                readers.Add(reader);
                
                var firstLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                currentLines.Add(firstLine != null ? LineData.FromString(firstLine, 0) : null);
            }

            // Merge lines from all readers
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Find the smallest line among all readers
                var minIndex = -1;
                LineData? minLine = null;

                for (var i = 0; i < currentLines.Count; i++)
                {
                    if (currentLines[i] == null) continue;

                    if (minLine == null || _comparer.Compare(currentLines[i]!.Value, minLine.Value) < 0)
                    {
                        minLine = currentLines[i];
                        minIndex = i;
                    }
                }

                // If no more lines, we're done
                if (minIndex == -1) break;

                // Write the smallest line to output
                await outputWriter.WriteLineAsync(minLine!.Value.Content, cancellationToken).ConfigureAwait(false);

                // Read next line from the reader that provided the smallest line
                var nextLine = await readers[minIndex].ReadLineAsync(cancellationToken).ConfigureAwait(false);
                currentLines[minIndex] = nextLine != null ? LineData.FromString(nextLine, 0) : null;
            }
        }
        finally
        {
            // Clean up readers
            foreach (var reader in readers)
            {
                reader.Dispose();
            }
        }

        return outputFile;
    }

    private void CleanupIntermediateFiles(List<string> files)
    {
        foreach (var file in files)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch (Exception)
            {
                // Ignore cleanup errors
            }
        }
    }

    public static int EstimateMergePasses(int chunkCount, int batchSize = 10)
    {
        if (chunkCount <= 1) return 0;
        return (int)Math.Ceiling(Math.Log(chunkCount, batchSize));
    }
} 