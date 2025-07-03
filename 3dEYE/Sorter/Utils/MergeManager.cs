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
            File.Copy(chunkFiles[0], outputFilePath, true);
            return;
        }

        var currentChunks = new List<string>(chunkFiles);
        var passNumber = 0;
        var tempDirectory = Path.GetDirectoryName(chunkFiles[0]) ?? Path.GetTempPath();

        while (currentChunks.Count > 1)
        {
            var nextChunks = new List<string>();
            var batchSize = Math.Min(10, currentChunks.Count);

            for (var i = 0; i < currentChunks.Count; i += batchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = currentChunks.Skip(i).Take(batchSize).ToList();
                var mergedFile = await MergeBatchAsync(batch, passNumber, tempDirectory, cancellationToken).ConfigureAwait(false);
                nextChunks.Add(mergedFile);
            }

            if (passNumber > 0) CleanupIntermediateFiles(currentChunks);

            currentChunks = nextChunks;
            passNumber++;
        }

        if (currentChunks.Count == 1) File.Move(currentChunks[0], outputFilePath, true);
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
        
        var readers = new List<StreamReader>();
        var currentLines = new List<LineData?>();
        
        try
        {
            foreach (var chunkFile in chunkFiles)
            {
                var reader = new StreamReader(chunkFile, Encoding.UTF8, true, bufferSize);
                readers.Add(reader);
                
                var firstLine = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                currentLines.Add(firstLine != null ? LineData.FromString(firstLine) : null);
            }

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

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

                if (minIndex == -1) break;

                await outputWriter.WriteLineAsync(minLine!.Value.Content, cancellationToken).ConfigureAwait(false);

                var nextLine = await readers[minIndex].ReadLineAsync(cancellationToken).ConfigureAwait(false);
                currentLines[minIndex] = nextLine != null ? LineData.FromString(nextLine) : null;
            }
        }
        finally
        {
            foreach (var reader in readers) reader.Dispose();
        }

        return outputFile;
    }

    private void CleanupIntermediateFiles(List<string> files)
    {
        foreach (var file in files)
        {
            if (File.Exists(file)) File.Delete(file);
        }
    }

    public static int EstimateMergePasses(int chunkCount, int batchSize = 10)
    {
        if (chunkCount <= 1) return 0;
        return (int)Math.Ceiling(Math.Log(chunkCount, batchSize));
    }
} 