# 3dEYE Sorting Algorithms

A high-performance sorting library with multiple algorithms optimized for different scenarios and file sizes.

## Overview

3dEYE provides three different sorting algorithms, each optimized for different scenarios:

1. **StreamingSorter** - Memory-efficient streaming sort with configurable memory limits
2. **ExternalMergeSorter** - Traditional external merge sort with optimized I/O
3. **ParallelExternalMergeSorter** - Parallel external merge sort utilizing multiple CPU cores

## Benchmark Results

### Test Environment
- **CPU**: Unknown processor (Windows 11)
- **OS**: Windows 11 (10.0.26100.4351/24H2/2024Update/HudsonValley)
- **Runtime**: .NET 9.0.6 (X64 RyuJIT AVX2)
- **Test Files**: 100MB and 1GB text files
- **Iterations**: 3 iterations with 1 warmup

### Performance Comparison

#### 100MB File Performance

| Algorithm | Buffer Size | Parallelism | Mean Time | Memory Allocated | Gen0 Collections | Performance Rank |
|-----------|-------------|-------------|-----------|------------------|------------------|------------------|
| **StreamingSorter** | 1MB | 50K lines | 9.57s | 768MB | 147,000 | **1st** |
| **StreamingSorter** | 10MB | 50K lines | 9.38s | 938MB | 147,000 | **2nd** |
| **ParallelExternalMergeSorter** | 10MB | 8 threads | 13.07s | 897MB | 796,000 | 3rd |
| **ParallelExternalMergeSorter** | 1MB | 8 threads | 14.28s | 1,102MB | 1,464,000 | 4th |
| **ExternalMergeSorter** | 10MB | N/A | 15.82s | 7,589MB | 354,000 | 5th |
| **ExternalMergeSorter** | 1MB | N/A | 18.69s | 7,899MB | 688,000 | 6th |

#### 1GB File Performance

| Algorithm | Buffer Size | Parallelism | Mean Time | Memory Allocated | Gen0 Collections | Performance Rank |
|-----------|-------------|-------------|-----------|------------------|------------------|------------------|
| **StreamingSorter** | 10MB | 50K lines | 97.17s | 7,655MB | 1,465,000 | **1st** |
| **StreamingSorter** | 1MB | 50K lines | 103.90s | 7,486MB | 1,465,000 | **2nd** |
| **ParallelExternalMergeSorter** | 1MB | 4 threads | 155.04s | 125,632MB | 18,983,000 | 3rd |
| **ParallelExternalMergeSorter** | 1MB | 8 threads | 152.91s | 125,764MB | 18,694,000 | 4th |
| **ExternalMergeSorter** | 10MB | N/A | 197.24s | 78,170MB | 4,764,000 | 5th |
| **ExternalMergeSorter** | 1MB | N/A | 235.07s | 83,131MB | 8,013,000 | 6th |

### Key Findings

#### 1. **StreamingSorter Dominates Performance**
- **Fastest for both file sizes** (9.57s vs 18.69s for 100MB, 97.17s vs 235.07s for 1GB)
- **Lowest memory allocation** (768MB vs 7,899MB for 100MB)
- **Most efficient garbage collection** (147K vs 688K Gen0 collections for 100MB)
- **Consistent performance** across different buffer sizes

#### 2. **ParallelExternalMergeSorter Shows Limited Benefits**
- **Moderate performance gains** over sequential version (13.07s vs 15.82s for 100MB)
- **Higher memory usage** due to parallel processing overhead
- **More garbage collections** indicating higher memory pressure
- **Diminishing returns** with higher parallelism (8 vs 4 threads)

#### 3. **ExternalMergeSorter Struggles with Memory**
- **Highest memory allocation** across all scenarios
- **Slowest performance** for both file sizes
- **Inefficient for medium-sized files** (100MB-1GB range)
- **Better suited for very large files** (>5GB) where memory constraints are critical

#### 4. **Buffer Size Impact**
- **Larger buffers (10MB)** generally improve performance for all algorithms
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
- Use **StreamingSorter** for best performance
- Configure 50K-100K memory lines based on available RAM
- Use 10MB buffer size for optimal I/O performance

### For Large Files (1GB - 5GB)
- Use **StreamingSorter** if sufficient memory is available
- Consider **ParallelExternalMergeSorter** for multi-core systems
- Use 8 threads and 10MB buffer size for parallel processing

### For Very Large Files (> 5GB)
- Use **ExternalMergeSorter** or **ParallelExternalMergeSorter**
- Configure large buffer sizes (10MB-50MB) for better I/O performance
- Monitor disk space for temporary files
- Use parallel processing with 4-8 threads depending on CPU cores

### Memory Considerations
- **StreamingSorter**: Requires memory proportional to `maxMemoryLines × average_line_length`
- **ExternalMergeSorter**: Memory usage = `buffer_size × number_of_chunks`
- **ParallelExternalMergeSorter**: Memory usage = `buffer_size × number_of_chunks × parallelism`

### Disk Space Requirements
- **StreamingSorter**: Minimal (single temporary file)
- **ExternalMergeSorter**: ~3x file size (input + output + temp files)
- **ParallelExternalMergeSorter**: ~3x file size (input + output + temp files)

## Algorithm Selection Guide

| File Size | Memory Available | Recommended Algorithm | Configuration |
|-----------|------------------|----------------------|---------------|
| < 100MB | Any | StreamingSorter | 50K lines, 1MB buffer |
| 100MB - 1GB | > 1GB | StreamingSorter | 100K lines, 10MB buffer |
| 1GB - 5GB | > 8GB | StreamingSorter | 100K lines, 10MB buffer |
| 1GB - 5GB | < 8GB | ParallelExternalMergeSorter | 8 threads, 10MB buffer |
| > 5GB | Any | ParallelExternalMergeSorter | 4-8 threads, 10MB buffer |
| > 10GB | Limited | ExternalMergeSorter | 10MB buffer | 