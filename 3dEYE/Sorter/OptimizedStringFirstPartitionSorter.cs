// using System.Collections.Concurrent;
// using System.Text;
// using Microsoft.Extensions.Logging;
// using _3dEYE.Sorter.Models;
// using System.Buffers;
//
// namespace _3dEYE.Sorter;
//
// public class OptimizedStringFirstPartitionSorter(
//     ILogger logger,
//     int maxMemoryLines = 100000,
//     int bufferSize = 1024 * 1024,
//     int maxDegreeOfParallelism = 0)
//     : ISorter
// {
//     private readonly int _maxDegreeOfParallelism = maxDegreeOfParallelism > 0 
//         ? maxDegreeOfParallelism 
//         : Environment.ProcessorCount;
//     
//     // Memory pool for char arrays to reduce allocations
//     private readonly ArrayPool<char> _charArrayPool = ArrayPool<char>.Shared;
//
//     public async Task SortAsync(
//         string inputFilePath, 
//         string outputFilePath, 
//         IComparer<LineData> comparer, 
//         CancellationToken cancellationToken = default)
//     {
//         await SortAsync(inputFilePath, outputFilePath, bufferSize, comparer, cancellationToken).ConfigureAwait(false);
//     }
//
//     public async Task SortAsync(
//         string inputFilePath, 
//         string outputFilePath, 
//         int bufferSizeBytes, 
//         IComparer<LineData> comparer, 
//         CancellationToken cancellationToken = default)
//     {
//         if (!File.Exists(inputFilePath))
//             throw new FileNotFoundException($"Input file not found: {inputFilePath}");
//
//         var fileInfo = new FileInfo(inputFilePath);
//         logger.LogInformation("Starting optimized string-first partition sort for file: {FilePath} ({Size} bytes) with {ThreadCount} threads", 
//             inputFilePath, fileInfo.Length, _maxDegreeOfParallelism);
//
//         var startTime = DateTime.UtcNow;
//
//         try
//         {
//             await PerformOptimizedStringFirstPartitionSortAsync(
//                 inputFilePath, 
//                 outputFilePath, 
//                 bufferSizeBytes, 
//                 comparer, 
//                 cancellationToken).ConfigureAwait(false);
//         }
//         catch (Exception ex)
//         {
//             logger.LogError(ex, "Error during optimized string-first partition sort");
//             throw;
//         }
//
//         var totalTime = DateTime.UtcNow - startTime;
//         var outputInfo = new FileInfo(outputFilePath);
//         logger.LogInformation("Optimized string-first partition sort completed in {TotalTime:g}. Output file: {OutputPath} ({Size} bytes)", 
//             totalTime, outputFilePath, outputInfo.Length);
//     }
//
//     private async Task PerformOptimizedStringFirstPartitionSortAsync(
//         string inputFilePath,
//         string outputFilePath,
//         int bufferSizeBytes,
//         IComparer<LineData> comparer,
//         CancellationToken cancellationToken)
//     {
//         // Create temporary directory for partition files in the same directory as output file
//         var outputDir = Path.GetDirectoryName(outputFilePath) ?? Path.GetTempPath();
//         var tempDir = Path.Combine(outputDir, $"optimized_string_partition_sort_{Guid.NewGuid():N}");
//         Directory.CreateDirectory(tempDir);
//
//         try
//         {
//             // Phase 1: Single-pass analysis with adaptive buffer sizing
//             logger.LogInformation("Phase 1: Single-pass string distribution analysis");
//             var prefixAnalysis = await AnalyzeStringDistributionSinglePassAsync(
//                 inputFilePath, bufferSizeBytes, cancellationToken).ConfigureAwait(false);
//
//             // Phase 2: Partition file by string prefixes with streaming
//             logger.LogInformation("Phase 2: Streaming partitioning by string prefixes");
//             var partitionFiles = await PartitionByStringPrefixesStreamingAsync(
//                 inputFilePath, tempDir, prefixAnalysis, bufferSizeBytes, cancellationToken).ConfigureAwait(false);
//
//             // Phase 3: Sort partitions and stream merge to output
//             logger.LogInformation("Phase 3: Sorting partitions and streaming merge to output");
//             await SortPartitionsAndStreamMergeAsync(
//                 partitionFiles, outputFilePath, comparer, bufferSizeBytes, cancellationToken).ConfigureAwait(false);
//         }
//         finally
//         {
//             // Clean up temporary directory
//             try
//             {
//                 if (Directory.Exists(tempDir))
//                 {
//                     Directory.Delete(tempDir, true);
//                 }
//             }
//             catch (Exception cleanupEx)
//             {
//                 logger.LogWarning("Failed to clean up temporary directory {TempDir}: {Message}", tempDir, cleanupEx.Message);
//             }
//         }
//     }
//
//     private async Task<PrefixAnalysis> AnalyzeStringDistributionSinglePassAsync(
//         string inputFilePath,
//         int bufferSizeBytes,
//         CancellationToken cancellationToken)
//     {
//         // Use custom prefix key to avoid string allocations
//         var prefixCounts = new ConcurrentDictionary<PrefixKey, long>();
//         var totalLines = 0L;
//         var maxPrefixLength = 3; // Start with 3-character prefixes
//
//         // Adaptive buffer sizing based on file size
//         var adaptiveBufferSize = CalculateAdaptiveBufferSize(inputFilePath, bufferSizeBytes);
//
//         using var reader = new StreamReader(inputFilePath, Encoding.UTF8, true, adaptiveBufferSize);
//         var lineBuffer = new char[8192]; // Reusable buffer for reading lines
//
//         while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
//         {
//             var lineLength = await reader.ReadLineAsync(lineBuffer, cancellationToken).ConfigureAwait(false);
//             if (lineLength == 0) break; // End of stream
//             
//             totalLines++;
//             
//             // Extract prefix using char arrays - NO STRING ALLOCATION!
//             var prefixKey = ExtractPrefixKeyFromChars(lineBuffer, lineLength, maxPrefixLength);
//             prefixCounts.AddOrUpdate(prefixKey, 1, (_, count) => count + 1);
//         }
//
//         // Calculate optimal prefix length based on actual distribution
//         var optimalPrefixLength = CalculateOptimalPrefixLengthFromActualData(prefixCounts, totalLines, maxMemoryLines);
//         
//         // Convert to string-based dictionary only for the final result
//         var optimalPrefixCounts = new Dictionary<string, long>();
//         foreach (var kvp in prefixCounts)
//         {
//             var prefixKey = kvp.Key;
//             var count = kvp.Value;
//             
//             // Convert prefix key to string only once
//             var prefixString = prefixKey.ToString();
//             
//             // If current prefix is shorter than optimal, use it as-is
//             if (prefixKey.Length <= optimalPrefixLength)
//             {
//                 optimalPrefixCounts[prefixString] = count;
//             }
//             else
//             {
//                 // If longer, truncate and aggregate
//                 var truncatedPrefix = prefixString[..optimalPrefixLength];
//                 if (optimalPrefixCounts.ContainsKey(truncatedPrefix))
//                     optimalPrefixCounts[truncatedPrefix] += count;
//                 else
//                     optimalPrefixCounts[truncatedPrefix] = count;
//             }
//         }
//
//         logger.LogInformation("Single-pass analysis complete: {TotalLines} lines, {PrefixCount} prefixes, optimal length: {PrefixLength}", 
//             totalLines, optimalPrefixCounts.Count, optimalPrefixLength);
//
//         return new PrefixAnalysis
//         {
//             TotalLines = totalLines,
//             PrefixCounts = optimalPrefixCounts,
//             OptimalPrefixLength = optimalPrefixLength
//         };
//     }
//
//     private async Task<List<string>> PartitionByStringPrefixesStreamingAsync(
//         string inputFilePath,
//         string tempDir,
//         PrefixAnalysis analysis,
//         int bufferSizeBytes,
//         CancellationToken cancellationToken)
//     {
//         var partitionFiles = new ConcurrentDictionary<string, string>();
//         var partitionWriters = new ConcurrentDictionary<string, StreamWriter>();
//
//         // Create partition files and writers with adaptive buffer sizing
//         var adaptiveBufferSize = CalculateAdaptiveBufferSize(inputFilePath, bufferSizeBytes);
//         
//         foreach (var prefix in analysis.PrefixCounts.Keys)
//         {
//             var partitionFile = Path.Combine(tempDir, $"partition_{prefix.ReplaceInvalidChars()}.txt");
//             partitionFiles[prefix] = partitionFile;
//             partitionWriters[prefix] = new StreamWriter(partitionFile, false, Encoding.UTF8, adaptiveBufferSize);
//         }
//
//         try
//         {
//             // Stream through input file and route lines to appropriate partitions - NO STRINGS!
//             using var reader = new StreamReader(inputFilePath, Encoding.UTF8, true, adaptiveBufferSize);
//             var lineBuffer = new char[8192];
//             var lineLength = await reader.ReadLineAsync(lineBuffer, cancellationToken).ConfigureAwait(false);
//             var linesProcessed = 0L;
//
//             while (lineLength > 0 && !cancellationToken.IsCancellationRequested)
//             {
//                 // Extract prefix using char arrays - NO STRING ALLOCATION!
//                 var prefixKey = ExtractPrefixKeyFromChars(lineBuffer, lineLength, analysis.OptimalPrefixLength);
//                 var prefixString = prefixKey.ToString();
//                 
//                 if (partitionWriters.TryGetValue(prefixString, out var writer))
//                 {
//                     await writer.WriteLineAsync(lineBuffer, 0, lineLength, cancellationToken).ConfigureAwait(false);
//                 }
//
//                 linesProcessed++;
//                 if (linesProcessed % 1000000 == 0)
//                 {
//                     logger.LogDebug("Processed {LinesProcessed} lines", linesProcessed);
//                 }
//
//                 lineLength = await reader.ReadLineAsync(lineBuffer, cancellationToken).ConfigureAwait(false);
//             }
//
//             logger.LogInformation("Streaming partitioning complete: {LinesProcessed} lines distributed to {PartitionCount} partitions", 
//                 linesProcessed, partitionFiles.Count);
//         }
//         finally
//         {
//             // Close all partition writers - FIXED: Proper disposal
//             foreach (var writer in partitionWriters.Values)
//             {
//                 try
//                 {
//                     await writer.DisposeAsync().ConfigureAwait(false);
//                 }
//                 catch (Exception ex)
//                 {
//                     logger.LogWarning("Failed to dispose partition writer: {Message}", ex.Message);
//                 }
//             }
//         }
//
//         return partitionFiles.Values.ToList();
//     }
//
//     // Custom prefix key struct to avoid string allocations
//     private readonly struct PrefixKey : IEquatable<PrefixKey>
//     {
//         private readonly char[] _chars;
//         private readonly int _length;
//         private readonly int _hashCode;
//
//         public PrefixKey(char[] chars, int length)
//         {
//             _chars = chars;
//             _length = length;
//             _hashCode = CalculateHashCode(chars, length);
//         }
//
//         public int Length => _length;
//
//         public override string ToString()
//         {
//             return new string(_chars, 0, _length);
//         }
//
//         public override bool Equals(object? obj)
//         {
//             return obj is PrefixKey other && Equals(other);
//         }
//
//         public bool Equals(PrefixKey other)
//         {
//             if (_length != other._length) return false;
//             
//             for (int i = 0; i < _length; i++)
//             {
//                 if (_chars[i] != other._chars[i]) return false;
//             }
//             return true;
//         }
//
//         public override int GetHashCode()
//         {
//             return _hashCode;
//         }
//
//         private static int CalculateHashCode(char[] chars, int length)
//         {
//             var hashCode = 0;
//             for (int i = 0; i < length; i++)
//             {
//                 hashCode = (hashCode * 31) + chars[i];
//             }
//             return hashCode;
//         }
//     }
//
//     // Extract prefix key from char array without creating strings
//     private static PrefixKey ExtractPrefixKeyFromChars(char[] lineBuffer, int lineLength, int maxLength)
//     {
//         // Extract the string part (after ". ") and get its prefix
//         var separatorIndex = -1;
//         for (int i = 0; i < lineLength - 1; i++)
//         {
//             if (lineBuffer[i] == '.' && lineBuffer[i + 1] == ' ')
//             {
//                 separatorIndex = i;
//                 break;
//             }
//         }
//         
//         if (separatorIndex == -1)
//         {
//             // If no separator, use the whole line as prefix
//             var prefixLength = Math.Min(lineLength, maxLength);
//             return new PrefixKey(lineBuffer, prefixLength);
//         }
//
//         var stringPartStart = separatorIndex + 2; // Skip ". "
//         var stringPartLength = lineLength - stringPartStart;
//         var prefixLength = Math.Min(stringPartLength, maxLength);
//         
//         // Create a new char array for the prefix
//         var prefixChars = new char[prefixLength];
//         Array.Copy(lineBuffer, stringPartStart, prefixChars, 0, prefixLength);
//         
//         return new PrefixKey(prefixChars, prefixLength);
//     }
//
//     private async Task SortPartitionsAndStreamMergeAsync(
//         List<string> partitionFiles,
//         string outputFilePath,
//         IComparer<LineData> comparer,
//         int bufferSizeBytes,
//         CancellationToken cancellationToken)
//     {
//         // Sort partitions in parallel and stream merge to output
//         var sortedPartitionFiles = new ConcurrentDictionary<string, string>();
//         
//         // Process partitions in parallel with limited concurrency to prevent memory explosion
//         var semaphore = new SemaphoreSlim(_maxDegreeOfParallelism, _maxDegreeOfParallelism);
//         var tasks = new List<Task>();
//         
//         foreach (var partitionFile in partitionFiles)
//         {
//             var task = Task.Run(async () =>
//             {
//                 await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
//                 try
//                 {
//                     var prefix = ExtractPrefixFromFilename(partitionFile);
//                     var sortedPartitionFile = await SortPartitionToFileAsync(partitionFile, comparer, bufferSizeBytes, cancellationToken).ConfigureAwait(false);
//                     sortedPartitionFiles[prefix] = sortedPartitionFile;
//                 }
//                 finally
//                 {
//                     semaphore.Release();
//                 }
//             }, cancellationToken);
//             
//             tasks.Add(task);
//         }
//
//         await Task.WhenAll(tasks).ConfigureAwait(false);
//         semaphore.Dispose();
//
//         // Stream merge sorted partitions to output
//         await StreamMergePartitionsAsync(sortedPartitionFiles, outputFilePath, bufferSizeBytes, cancellationToken).ConfigureAwait(false);
//
//         logger.LogInformation("Sorting and streaming merge complete: {PartitionCount} partitions merged to output", sortedPartitionFiles.Count);
//     }
//
//     private async Task<string> SortPartitionToFileAsync(
//         string partitionFile,
//         IComparer<LineData> comparer,
//         int bufferSizeBytes,
//         CancellationToken cancellationToken)
//     {
//         var partitionInfo = new FileInfo(partitionFile);
//         var partitionSizeMB = partitionInfo.Length / (1024 * 1024);
//         
//         // For small partitions (< 100MB), use in-memory sort
//         if (partitionSizeMB < 100)
//         {
//             return await SortPartitionInMemoryAsync(partitionFile, comparer, bufferSizeBytes, cancellationToken);
//         }
//         
//         // For large partitions, use external merge sort
//         logger.LogDebug("Partition {PartitionFile} is {SizeMB}MB, using external merge sort", 
//             Path.GetFileName(partitionFile), partitionSizeMB);
//         
//         return await SortPartitionWithExternalMergeAsync(partitionFile, comparer, bufferSizeBytes, cancellationToken);
//     }
//
//     private async Task<string> SortPartitionInMemoryAsync(
//         string partitionFile,
//         IComparer<LineData> comparer,
//         int bufferSizeBytes,
//         CancellationToken cancellationToken)
//     {
//         // Use char arrays directly for better memory efficiency
//         var lines = new List<char[]>();
//         var currentPosition = 0L;
//
//         // Read partition into memory using char arrays directly - NO STRINGS!
//         using var reader = new StreamReader(partitionFile, Encoding.UTF8, true, bufferSizeBytes);
//         var lineBuffer = new char[8192]; // Reusable buffer for reading lines
//         
//         while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
//         {
//             var lineLength = await reader.ReadLineAsync(lineBuffer, cancellationToken).ConfigureAwait(false);
//             if (lineLength == 0) break; // End of stream
//             
//             // Rent char array from pool for memory efficiency
//             var lineChars = _charArrayPool.Rent(lineLength);
//             Array.Copy(lineBuffer, lineChars, lineLength);
//             lines.Add(lineChars);
//             currentPosition = reader.BaseStream.Position;
//         }
//
//         // Sort using a custom comparer that works with char arrays
//         lines.Sort(new CharArrayComparer(comparer));
//         
//         // Write sorted partition to temporary file
//         var sortedPartitionFile = partitionFile.Replace(".txt", "_sorted.txt");
//         await using var writer = new StreamWriter(sortedPartitionFile, false, Encoding.UTF8, bufferSizeBytes);
//         
//         foreach (var lineChars in lines)
//         {
//             // Find the actual length of the char array (trim nulls)
//             var actualLength = Array.IndexOf(lineChars, '\0');
//             if (actualLength == -1) actualLength = lineChars.Length;
//             
//             await writer.WriteLineAsync(lineChars, 0, actualLength, cancellationToken).ConfigureAwait(false);
//             
//             // Return char array to pool
//             _charArrayPool.Return(lineChars);
//         }
//
//         // Clear the list to free memory immediately
//         lines.Clear();
//         lines.TrimExcess();
//
//         logger.LogDebug("In-memory sorted partition {PartitionFile}: {LineCount} lines", 
//             Path.GetFileName(partitionFile), lines.Count);
//
//         return sortedPartitionFile;
//     }
//
//     private async Task<string> SortPartitionWithExternalMergeAsync(
//         string partitionFile,
//         IComparer<LineData> comparer,
//         int bufferSizeBytes,
//         CancellationToken cancellationToken)
//     {
//         var tempDir = Path.GetDirectoryName(partitionFile) ?? Path.GetTempPath();
//         var chunkSize = maxMemoryLines; // Use the configured memory limit for chunks
//         
//         // Phase 1: Split partition into sorted chunks using char arrays - NO STRINGS!
//         var chunkFiles = new List<string>();
//         var chunkIndex = 0;
//         
//         using var reader = new StreamReader(partitionFile, Encoding.UTF8, true, bufferSizeBytes);
//         var lines = new List<char[]>();
//         var currentPosition = 0L;
//         var lineBuffer = new char[8192]; // Reusable buffer for reading lines
//         
//         while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
//         {
//             var lineLength = await reader.ReadLineAsync(lineBuffer, cancellationToken).ConfigureAwait(false);
//             if (lineLength == 0) break; // End of stream
//             
//             // Rent char array from pool for memory efficiency
//             var lineChars = _charArrayPool.Rent(lineLength);
//             Array.Copy(lineBuffer, lineChars, lineLength);
//             lines.Add(lineChars);
//             currentPosition = reader.BaseStream.Position;
//             
//             // When chunk is full, sort and write to file
//             if (lines.Count >= chunkSize)
//             {
//                 var chunkFile = await WriteSortedChunkAsync(lines, tempDir, chunkIndex, bufferSizeBytes, comparer, cancellationToken);
//                 chunkFiles.Add(chunkFile);
//                 chunkIndex++;
//                 
//                 // Return char arrays to pool
//                 foreach (var lineChars in lines)
//                 {
//                     _charArrayPool.Return(lineChars);
//                 }
//                 
//                 lines.Clear();
//                 lines.TrimExcess();
//             }
//         }
//         
//         // Write remaining lines as final chunk
//         if (lines.Count > 0)
//         {
//             var chunkFile = await WriteSortedChunkAsync(lines, tempDir, chunkIndex, bufferSizeBytes, comparer, cancellationToken);
//             chunkFiles.Add(chunkFile);
//             
//             // Return char arrays to pool
//             foreach (var lineChars in lines)
//             {
//                 _charArrayPool.Return(lineChars);
//             }
//         }
//         
//         // Phase 2: Merge chunks into final sorted file
//         var sortedPartitionFile = partitionFile.Replace(".txt", "_sorted.txt");
//         await MergeChunksAsync(chunkFiles, sortedPartitionFile, bufferSizeBytes, comparer, cancellationToken);
//         
//         // Clean up chunk files
//         foreach (var chunkFile in chunkFiles)
//         {
//             try
//             {
//                 if (File.Exists(chunkFile))
//                     File.Delete(chunkFile);
//             }
//             catch (Exception ex)
//             {
//                 logger.LogWarning("Failed to cleanup chunk file {ChunkFile}: {Message}", chunkFile, ex.Message);
//             }
//         }
//         
//         logger.LogDebug("External merge sorted partition {PartitionFile}: {ChunkCount} chunks", 
//             Path.GetFileName(partitionFile), chunkFiles.Count);
//         
//         return sortedPartitionFile;
//     }
//
//     private async Task<string> WriteSortedChunkAsync(
//         List<char[]> lines,
//         string tempDir,
//         int chunkIndex,
//         int bufferSizeBytes,
//         IComparer<LineData> comparer,
//         CancellationToken cancellationToken)
//     {
//         // Sort the chunk in memory using char arrays
//         lines.Sort(new CharArrayComparer(comparer));
//         
//         // Write sorted chunk to file
//         var chunkFile = Path.Combine(tempDir, $"chunk_{chunkIndex:D6}.tmp");
//         await using var writer = new StreamWriter(chunkFile, false, Encoding.UTF8, bufferSizeBytes);
//         
//         foreach (var lineChars in lines)
//         {
//             // Find the actual length of the char array (trim nulls)
//             var actualLength = Array.IndexOf(lineChars, '\0');
//             if (actualLength == -1) actualLength = lineChars.Length;
//             
//             await writer.WriteLineAsync(lineChars, 0, actualLength, cancellationToken).ConfigureAwait(false);
//         }
//         
//         return chunkFile;
//     }
//
//     private async Task MergeChunksAsync(
//         List<string> chunkFiles,
//         string outputFile,
//         int bufferSizeBytes,
//         IComparer<LineData> comparer,
//         CancellationToken cancellationToken)
//     {
//         if (chunkFiles.Count == 1)
//         {
//             // Single chunk, just rename
//             File.Move(chunkFiles[0], outputFile, true);
//             return;
//         }
//         
//         // Multi-pass merge: merge pairs of chunks until we have one final file
//         var currentChunks = chunkFiles.ToList();
//         var passNumber = 0;
//         
//         while (currentChunks.Count > 1)
//         {
//             var mergedChunks = new List<string>();
//             var tempDir = Path.GetDirectoryName(outputFile) ?? Path.GetTempPath();
//             
//             // Merge chunks in pairs
//             for (int i = 0; i < currentChunks.Count; i += 2)
//             {
//                 if (i + 1 < currentChunks.Count)
//                 {
//                     // Merge two chunks
//                     var mergedChunk = Path.Combine(tempDir, $"merged_pass{passNumber:D3}_{i / 2:D6}.tmp");
//                     await MergeTwoChunksAsync(currentChunks[i], currentChunks[i + 1], mergedChunk, bufferSizeBytes, comparer, cancellationToken);
//                     mergedChunks.Add(mergedChunk);
//                 }
//                 else
//                 {
//                     // Single chunk, copy as-is
//                     var copiedChunk = Path.Combine(tempDir, $"merged_pass{passNumber:D3}_{i / 2:D6}.tmp");
//                     File.Copy(currentChunks[i], copiedChunk, true);
//                     mergedChunks.Add(copiedChunk);
//                 }
//             }
//             
//             // Clean up previous pass chunks
//             foreach (var chunk in currentChunks)
//             {
//                 try
//                 {
//                     if (File.Exists(chunk))
//                         File.Delete(chunk);
//                 }
//                 catch (Exception ex)
//                 {
//                     logger.LogWarning("Failed to cleanup chunk {Chunk}: {Message}", chunk, ex.Message);
//                 }
//             }
//             
//             currentChunks = mergedChunks;
//             passNumber++;
//         }
//         
//         // Rename final chunk to output file
//         if (currentChunks.Count == 1)
//         {
//             File.Move(currentChunks[0], outputFile, true);
//         }
//     }
//
//     private async Task MergeTwoChunksAsync(
//         string chunk1Path,
//         string chunk2Path,
//         string outputPath,
//         int bufferSizeBytes,
//         IComparer<LineData> comparer,
//         CancellationToken cancellationToken)
//     {
//         await using var writer = new StreamWriter(outputPath, false, Encoding.UTF8, bufferSizeBytes);
//         using var reader1 = new StreamReader(chunk1Path, Encoding.UTF8, true, bufferSizeBytes);
//         using var reader2 = new StreamReader(chunk2Path, Encoding.UTF8, true, bufferSizeBytes);
//         
//         var lineBuffer1 = new char[8192];
//         var lineBuffer2 = new char[8192];
//         
//         var line1Length = await reader1.ReadLineAsync(lineBuffer1, cancellationToken).ConfigureAwait(false);
//         var line2Length = await reader2.ReadLineAsync(lineBuffer2, cancellationToken).ConfigureAwait(false);
//         
//         while (line1Length > 0 && line2Length > 0 && !cancellationToken.IsCancellationRequested)
//         {
//             // Rent char arrays from pool for memory efficiency
//             var line1Chars = _charArrayPool.Rent(line1Length);
//             var line2Chars = _charArrayPool.Rent(line2Length);
//             
//             Array.Copy(lineBuffer1, line1Chars, line1Length);
//             Array.Copy(lineBuffer2, line2Chars, line2Length);
//             
//             var lineData1 = new LineData(line1Chars.AsMemory(0, line1Length), 0);
//             var lineData2 = new LineData(line2Chars.AsMemory(0, line2Length), 0);
//             var comparison = comparer.Compare(lineData1, lineData2);
//             
//             if (comparison <= 0)
//             {
//                 await writer.WriteLineAsync(line1Chars, 0, line1Length, cancellationToken).ConfigureAwait(false);
//                 line1Length = await reader1.ReadLineAsync(lineBuffer1, cancellationToken).ConfigureAwait(false);
//             }
//             else
//             {
//                 await writer.WriteLineAsync(line2Chars, 0, line2Length, cancellationToken).ConfigureAwait(false);
//                 line2Length = await reader2.ReadLineAsync(lineBuffer2, cancellationToken).ConfigureAwait(false);
//             }
//             
//             // Return char arrays to pool
//             _charArrayPool.Return(line1Chars);
//             _charArrayPool.Return(line2Chars);
//         }
//         
//         // Write remaining lines from chunk1
//         while (line1Length > 0 && !cancellationToken.IsCancellationRequested)
//         {
//             var line1Chars = _charArrayPool.Rent(line1Length);
//             Array.Copy(lineBuffer1, line1Chars, line1Length);
//             await writer.WriteLineAsync(line1Chars, 0, line1Length, cancellationToken).ConfigureAwait(false);
//             _charArrayPool.Return(line1Chars);
//             line1Length = await reader1.ReadLineAsync(lineBuffer1, cancellationToken).ConfigureAwait(false);
//         }
//         
//         // Write remaining lines from chunk2
//         while (line2Length > 0 && !cancellationToken.IsCancellationRequested)
//         {
//             var line2Chars = _charArrayPool.Rent(line2Length);
//             Array.Copy(lineBuffer2, line2Chars, line2Length);
//             await writer.WriteLineAsync(line2Chars, 0, line2Length, cancellationToken).ConfigureAwait(false);
//             _charArrayPool.Return(line2Chars);
//             line2Length = await reader2.ReadLineAsync(lineBuffer2, cancellationToken).ConfigureAwait(false);
//         }
//     }
//
//     private async Task StreamMergePartitionsAsync(
//         ConcurrentDictionary<string, string> sortedPartitionFiles,
//         string outputFilePath,
//         int bufferSizeBytes,
//         CancellationToken cancellationToken)
//     {
//         // Stream merge by reading from sorted partitions in alphabetical order
//         await using var outputWriter = new StreamWriter(outputFilePath, false, Encoding.UTF8, bufferSizeBytes);
//         
//         var sortedPrefixes = sortedPartitionFiles.Keys.OrderBy(p => p).ToList();
//         
//         foreach (var prefix in sortedPrefixes)
//         {
//             if (sortedPartitionFiles.TryGetValue(prefix, out var sortedPartitionFile))
//             {
//                 // Stream copy the sorted partition to output using char arrays from pool - NO STRINGS!
//                 using var reader = new StreamReader(sortedPartitionFile, Encoding.UTF8, true, bufferSizeBytes);
//                 var lineBuffer = new char[8192];
//                 var lineLength = await reader.ReadLineAsync(lineBuffer, cancellationToken).ConfigureAwait(false);
//                 
//                 while (lineLength > 0 && !cancellationToken.IsCancellationRequested)
//                 {
//                     var lineChars = _charArrayPool.Rent(lineLength);
//                     Array.Copy(lineBuffer, lineChars, lineLength);
//                     await outputWriter.WriteLineAsync(lineChars, 0, lineLength, cancellationToken).ConfigureAwait(false);
//                     _charArrayPool.Return(lineChars);
//                     lineLength = await reader.ReadLineAsync(lineBuffer, cancellationToken).ConfigureAwait(false);
//                 }
//             }
//         }
//     }
//
//     private static string ExtractPrefixFromFilename(string filename)
//     {
//         // Extract prefix from filename like "partition_ABC.txt"
//         var fileName = Path.GetFileNameWithoutExtension(filename);
//         return fileName.Replace("partition_", "");
//     }
//
//     private static int CalculateOptimalPrefixLengthFromActualData(
//         ConcurrentDictionary<PrefixKey, long> prefixCounts, 
//         long totalLines, 
//         int maxMemoryLines)
//     {
//         // Calculate optimal prefix length based on actual data distribution
//         var currentLength = 1;
//         var maxLength = 5; // Maximum prefix length to consider
//
//         for (var length = 1; length <= maxLength; length++)
//         {
//             // Group prefixes by their first 'length' characters
//             var groupedPrefixes = new Dictionary<string, long>();
//             
//             foreach (var kvp in prefixCounts)
//             {
//                 var prefixKey = kvp.Key;
//                 var count = kvp.Value;
//                 
//                 // Convert prefix key to string for grouping
//                 var prefixString = prefixKey.ToString();
//                 var groupKey = prefixString.Length >= length ? prefixString[..length] : prefixString;
//                 
//                 if (groupedPrefixes.ContainsKey(groupKey))
//                     groupedPrefixes[groupKey] += count;
//                 else
//                     groupedPrefixes[groupKey] = count;
//             }
//
//             var avgLinesPerPartition = totalLines / groupedPrefixes.Count;
//
//             if (avgLinesPerPartition <= maxMemoryLines)
//             {
//                 currentLength = length;
//                 break;
//             }
//         }
//
//         return currentLength;
//     }
//
//     private static int CalculateAdaptiveBufferSize(string inputFilePath, int defaultBufferSize)
//     {
//         var fileInfo = new FileInfo(inputFilePath);
//         var fileSizeMB = fileInfo.Length / (1024 * 1024);
//
//         // Adaptive buffer sizing based on file size - FIXED: More conservative sizing
//         return fileSizeMB switch
//         {
//             < 100 => Math.Min(defaultBufferSize, 512 * 1024), // 512KB for small files
//             < 1000 => Math.Min(defaultBufferSize, 2 * 1024 * 1024), // 2MB for medium files
//             _ => Math.Min(defaultBufferSize, 5 * 1024 * 1024) // 5MB for large files, but capped
//         };
//     }
//
//     public Task<SortStatistics> GetSortStatisticsAsync(
//         string inputFilePath, 
//         int bufferSizeBytes)
//     {
//         if (!File.Exists(inputFilePath))
//             throw new FileNotFoundException($"Input file not found: {inputFilePath}");
//
//         var fileInfo = new FileInfo(inputFilePath);
//         
//         // Estimate based on typical string distribution
//         var estimatedPrefixes = 100; // Conservative estimate
//         var estimatedLinesPerPartition = fileInfo.Length / (estimatedPrefixes * 200); // Assume 200 bytes per line
//         
//         var statistics = new SortStatistics
//         {
//             FileSizeBytes = fileInfo.Length,
//             BufferSizeBytes = bufferSizeBytes,
//             EstimatedMergePasses = 0, // Single-pass merge
//             EstimatedTotalIOPerFile = 3 // Read once, partition write, merge write
//         };
//
//         return Task.FromResult(statistics);
//     }
//
//     private class PrefixAnalysis
//     {
//         public long TotalLines { get; set; }
//         public Dictionary<string, long> PrefixCounts { get; set; } = new();
//         public int OptimalPrefixLength { get; set; }
//     }
//
//     // Custom comparer that works with char arrays to avoid LineData object creation
//     private class CharArrayComparer : IComparer<char[]>
//     {
//         private readonly IComparer<LineData> _lineDataComparer;
//
//         public CharArrayComparer(IComparer<LineData> lineDataComparer)
//         {
//             _lineDataComparer = lineDataComparer;
//         }
//
//         public int Compare(char[]? x, char[]? y)
//         {
//             if (x == null && y == null) return 0;
//             if (x == null) return -1;
//             if (y == null) return 1;
//
//             // Create temporary LineData objects only for comparison
//             var lineDataX = new LineData(x.AsMemory(), 0);
//             var lineDataY = new LineData(y.AsMemory(), 0);
//             
//             return _lineDataComparer.Compare(lineDataX, lineDataY);
//         }
//     }
// }
//
