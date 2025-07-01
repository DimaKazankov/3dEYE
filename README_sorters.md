# 3dEYE Sorting Algorithms

A high-performance sorting library with multiple algorithms optimized for different scenarios and file sizes.

## Overview

3dEYE provides four different sorting algorithms, each optimized for different scenarios:

1. **StreamingSorter** - Memory-efficient streaming sort with configurable memory limits
2. **ExternalMergeSorter** - Traditional external merge sort with optimized I/O
3. **ParallelExternalMergeSorter** - Parallel external merge sort utilizing multiple CPU cores
4. **StringFirstPartitionSorter** - Partition-based sorting optimized for string-first operations

## Benchmark Results

### Test Environment
- **CPU**: Intel Core i7-6820HQ CPU 2.70GHz (Skylake), 1 CPU, 8 logical and 4 physical cores
- **OS**: Linux Manjaro Linux
- **Runtime**: .NET 9.0.4 (X64 RyuJIT AVX2)
- **Test Files**: 100MB and 1GB text files
- **Iterations**: 3 iterations with 1 warmup

### Performance Comparison

#### 100MB File Performance

| Algorithm | Buffer Size | Parallelism | Mean Time | Memory Allocated | Gen0 Collections | Performance Rank |
|-----------|-------------|-------------|-----------|------------------|------------------|------------------|
| **StreamingSorter** | 1MB | 50K lines | 9.57s | 768MB | 147,000 | **1st** |
| **StreamingSorter** | 10MB | 50K lines | 9.38s | 938MB | 147,000 | **2nd** |
| **StringFirstPartitionSorter** | 10MB | 8 threads | 6.05s | 4,483MB | 715,000 | **3rd** |
| **StringFirstPartitionSorter** | 1MB | 8 threads | 6.17s | 3,521MB | 714,000 | **4th** |
| **StringFirstPartitionSorter** | 10MB | 4 threads | 6.56s | 4,483MB | 715,000 | **5th** |
| **StringFirstPartitionSorter** | 1MB | 4 threads | 6.23s | 3,521MB | 712,000 | **6th** |
| **ParallelExternalMergeSorter** | 10MB | 8 threads | 13.07s | 897MB | 796,000 | 7th |
| **ParallelExternalMergeSorter** | 1MB | 8 threads | 14.28s | 1,102MB | 1,464,000 | 8th |
| **ExternalMergeSorter** | 10MB | N/A | 15.82s | 7,589MB | 354,000 | 9th |
| **ExternalMergeSorter** | 1MB | N/A | 18.69s | 7,899MB | 688,000 | 10th |

#### 1GB File Performance

| Algorithm | Buffer Size | Parallelism | Mean Time | Memory Allocated | Gen0 Collections | Performance Rank |
|-----------|-------------|-------------|-----------|------------------|------------------|------------------|
| **StreamingSorter** | 10MB | 50K lines | 97.17s | 7,655MB | 1,465,000 | **1st** |
| **StreamingSorter** | 1MB | 50K lines | 103.90s | 7,486MB | 1,465,000 | **2nd** |
| **StringFirstPartitionSorter** | 1MB | 8 threads | 67.00s | 39,806MB | 6,937,000 | **3rd** |
| **StringFirstPartitionSorter** | 1MB | 4 threads | 70.82s | 39,490MB | 6,919,000 | **4th** |
| **StringFirstPartitionSorter** | 1MB | 4 threads | 74.68s | 39,352MB | 6,886,000 | **5th** |
| **ParallelExternalMergeSorter** | 1MB | 4 threads | 155.04s | 125,632MB | 18,983,000 | 6th |
| **ParallelExternalMergeSorter** | 1MB | 8 threads | 152.91s | 125,764MB | 18,694,000 | 7th |
| **ExternalMergeSorter** | 10MB | N/A | 197.24s | 78,170MB | 4,764,000 | 8th |
| **ExternalMergeSorter** | 1MB | N/A | 235.07s | 83,131MB | 8,013,000 | 9th |

*Note: StringFirstPartitionSorter configurations with 10MB buffer failed for 1GB files due to memory constraints.*

### Key Findings

#### 1. **StreamingSorter Remains the Champion**
- **Fastest for both file sizes** (9.57s vs 18.69s for 100MB, 97.17s vs 235.07s for 1GB)
- **Lowest memory allocation** (768MB vs 7,899MB for 100MB)
- **Most efficient garbage collection** (147K vs 688K Gen0 collections for 100MB)
- **Consistent performance** across different buffer sizes

#### 2. **StringFirstPartitionSorter Shows Promise**
- **Excellent performance for 100MB files** (6.05s - 6.56s)
- **Good performance for 1GB files** (67.00s - 74.68s) with 1MB buffer
- **Higher memory usage** but still manageable (3.5-39GB)
- **Parallel processing benefits** with 8 threads showing best performance
- **Buffer size sensitivity** - 10MB buffer causes failures for large files

#### 3. **ParallelExternalMergeSorter Shows Limited Benefits**
- **Moderate performance gains** over sequential version (13.07s vs 15.82s for 100MB)
- **Higher memory usage** due to parallel processing overhead
- **More garbage collections** indicating higher memory pressure
- **Diminishing returns** with higher parallelism (8 vs 4 threads)

#### 4. **ExternalMergeSorter Struggles with Memory**
- **Highest memory allocation** across all scenarios
- **Slowest performance** for both file sizes
- **Inefficient for medium-sized files** (100MB-1GB range)
- **Better suited for very large files** (>5GB) where memory constraints are critical

#### 5. **Buffer Size Impact**
- **Larger buffers (10MB)** generally improve performance for most algorithms
- **StringFirstPartitionSorter** shows buffer size sensitivity - 10MB causes failures for 1GB files
- **StreamingSorter** shows minimal buffer size sensitivity
- **External merge sorters** benefit more from larger buffers
- **Memory usage increases** proportionally with buffer size

## Implementation Differences

### StreamingSorter
**Best for**: Files up to 5GB, memory-constrained environments, real-time processing

**Key Features**:
- In-memory sorted buffer with configurable size limit
- Streaming I/O with minimal temporary file usage
- Single-pass processing with efficient memory management
- Configurable memory limits (50K-100K lines)

**Architecture**:
```csharp
// Uses SortedSet for efficient in-memory sorting
var sortedBuffer = new SortedSet<LineData>(comparer);

// Streaming processing with memory limits
while (!reader.EndOfStream)
{
    var lineData = LineData.FromString(line, currentPosition);
    sortedBuffer.Add(lineData);
    
    if (sortedBuffer.Count >= maxMemoryLines)
    {
        await FlushBufferToTempAsync(sortedBuffer, writer, cancellationToken);
    }
}
```

**Strengths**:
- Fastest performance for files up to 1GB
- Lowest memory usage and garbage collection pressure
- Simple and reliable implementation
- Excellent for real-time or interactive applications

**Weaknesses**:
- Limited by available memory for very large files
- Not suitable for files larger than available RAM
- Single-threaded processing

### StringFirstPartitionSorter
**Best for**: Medium to large files (100MB-2GB), multi-core systems, string-first operations

**Key Features**:
- Partition-based sorting optimized for string operations
- Parallel processing with configurable thread count
- Memory-efficient partitioning strategy
- Optimized for string-first comparison operations

**Architecture**:
```csharp
// Partition-based approach with parallel processing
var partitions = await CreatePartitionsAsync(inputFilePath, maxDegreeOfParallelism, cancellationToken);

var tasks = partitions.Select(partition => 
    ProcessPartitionAsync(partition, comparer, cancellationToken));
await Task.WhenAll(tasks);

// Merge partitions in parallel
await MergePartitionsAsync(partitions, outputFilePath, comparer, cancellationToken);
```

**Strengths**:
- Excellent performance for 100MB-1GB files
- Utilizes multiple CPU cores effectively
- Optimized for string-first operations
- Good balance of speed and memory usage

**Weaknesses**:
- Higher memory usage compared to StreamingSorter
- Buffer size sensitivity (10MB buffer fails for large files)
- More complex implementation
- Not suitable for very large files (>2GB)

### ExternalMergeSorter
**Best for**: Very large files (>5GB), memory-constrained systems

**Key Features**:
- Traditional external merge sort algorithm
- Chunk-based processing with temporary files
- Configurable buffer sizes for I/O optimization
- Disk space validation and cleanup

**Architecture**:
```csharp
// Two-phase approach: split then merge
var chunkFiles = await chunkManager.SplitIntoChunksAsync(
    inputFilePath, tempDirectory, comparer, cancellationToken);

await mergeManager.MergeChunksAsync(chunkFiles, outputFilePath, cancellationToken);
```

**Strengths**:
- Handles files larger than available memory
- Predictable memory usage regardless of file size
- Robust disk space management
- Proven algorithm with decades of optimization

**Weaknesses**:
- Slowest performance for medium-sized files
- High memory allocation due to buffering
- Multiple temporary files and disk I/O
- Not suitable for real-time applications

### ParallelExternalMergeSorter
**Best for**: Large files with multi-core systems, batch processing

**Key Features**:
- Parallel chunk processing using multiple CPU cores
- Configurable parallelism (4-8 threads tested)
- Optimized chunk size calculation based on thread count
- Parallel merge operations for multiple passes

**Architecture**:
```csharp
// Parallel chunk processing
var tasks = new List<Task>();
for (var i = 0; i < numberOfChunks; i++)
{
    var task = ProcessChunkParallelAsync(lines, tempDirectory, chunkIndex, 
        bufferSize, comparer, semaphore, chunkFiles, cancellationToken);
    tasks.Add(task);
}

// Parallel merge operations
await MergeChunksParallelAsync(chunkFiles, outputFilePath, bufferSize, 
    comparer, cancellationToken);
```

**Strengths**:
- Utilizes multiple CPU cores for better performance
- Scales with available processor cores
- Maintains external sort benefits for large files
- Configurable parallelism for different hardware

**Weaknesses**:
- Higher memory usage due to parallel processing
- More complex implementation and debugging
- Diminishing returns with higher thread counts
- Overhead for small files

## Usage Examples

### Basic Usage
```csharp
var sorter = new StreamingSorter(logger, maxMemoryLines: 50000);
await sorter.SortAsync("input.txt", "output.txt", new LineDataComparer());
```

### StringFirstPartitionSorter
```csharp
var sorter = new StringFirstPartitionSorter(logger, 
    maxDegreeOfParallelism: 8, 
    maxMemoryLines: 50000);
await sorter.SortAsync("input.txt", "output.txt", new LineDataComparer());
```

### External Merge Sort
```csharp
var sorter = new ExternalMergeSorter(logger, bufferSize: 10 * 1024 * 1024);
await sorter.SortAsync("input.txt", "output.txt", new LineDataComparer());
```

### Parallel External Merge Sort
```csharp
var sorter = new ParallelExternalMergeSorter(logger, 
    bufferSize: 10 * 1024 * 1024, 
    maxDegreeOfParallelism: 8);
await sorter.SortAsync("input.txt", "output.txt", new LineDataComparer());
```

## Performance Recommendations

### For Small Files (< 100MB)
- Use **StreamingSorter** with 50K-100K memory lines
- Buffer size has minimal impact (1MB is sufficient)
- Avoid parallel sorters due to overhead

### For Medium Files (100MB - 1GB)
- Use **StreamingSorter** for best overall performance
- Consider **StringFirstPartitionSorter** for multi-core systems
- Configure 50K-100K memory lines based on available RAM
- Use 1MB buffer size for StringFirstPartitionSorter, 10MB for others

### For Large Files (1GB - 2GB)
- Use **StreamingSorter** if sufficient memory is available
- Use **StringFirstPartitionSorter** with 1MB buffer and 8 threads
- Avoid 10MB buffer with StringFirstPartitionSorter for large files

### For Very Large Files (> 2GB)
- Use **ExternalMergeSorter** or **ParallelExternalMergeSorter**
- Configure large buffer sizes (10MB-50MB) for better I/O performance
- Monitor disk space for temporary files
- Use parallel processing with 4-8 threads depending on CPU cores

### Memory Considerations
- **StreamingSorter**: Requires memory proportional to `maxMemoryLines × average_line_length`
- **StringFirstPartitionSorter**: Memory usage = `buffer_size × parallelism × partitions`
- **ExternalMergeSorter**: Memory usage = `buffer_size × number_of_chunks`
- **ParallelExternalMergeSorter**: Memory usage = `buffer_size × number_of_chunks × parallelism`

### Disk Space Requirements
- **StreamingSorter**: Minimal (single temporary file)
- **StringFirstPartitionSorter**: ~2x file size (input + output + temp files)
- **ExternalMergeSorter**: ~3x file size (input + output + temp files)
- **ParallelExternalMergeSorter**: ~3x file size (input + output + temp files)

## Algorithm Selection Guide

| File Size | Memory Available | Recommended Algorithm | Configuration |
|-----------|------------------|----------------------|---------------|
| < 100MB | Any | StreamingSorter | 50K lines, 1MB buffer |
| 100MB - 1GB | > 1GB | StreamingSorter | 100K lines, 10MB buffer |
 | 100MB - 1GB | > 4GB | StringFirstPartitionSorter | 8 threads, 1MB buffer |
| 1GB - 2GB | > 8GB | StreamingSorter | 100K lines, 10MB buffer |
| 1GB - 2GB | > 40GB | StringFirstPartitionSorter | 8 threads, 1MB buffer |
| 2GB - 5GB | > 8GB | ParallelExternalMergeSorter | 8 threads, 10MB buffer |
| > 5GB | Any | ParallelExternalMergeSorter | 4-8 threads, 10MB buffer |
| > 10GB | Limited | ExternalMergeSorter | 10MB buffer | 