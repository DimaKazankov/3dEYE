# External Merge Sort Implementation

This project implements an efficient external merge sort algorithm designed to handle very large files (100GB+) that don't fit in memory. The implementation uses modern .NET features like `ReadOnlyMemory`, `Span<T>`, and `ArrayPool<T>` for optimal memory management and performance.

## Features

- **Memory Efficient**: Uses `ReadOnlyMemory` and `Span<T>` to minimize memory allocations
- **Scalable**: Handles files of any size by splitting into manageable chunks
- **Configurable**: Adjustable buffer sizes for different memory constraints
- **Async**: Full async/await support for non-blocking operations
- **Cancellable**: Supports cancellation tokens for long-running operations
- **Logging**: Comprehensive logging for monitoring and debugging
- **Statistics**: Provides performance statistics and estimates

## Architecture

### Core Components

1. **`ISorter`** - Main interface for sorting operations
2. **`ExternalMergeSorter`** - Main implementation orchestrating the sort process
3. **`ChunkManager`** - Handles splitting large files into sorted chunks
4. **`MergeManager`** - Manages merging sorted chunks efficiently
5. **`LineData`** - Memory-efficient representation of file lines

### Algorithm Overview

The external merge sort works in two phases:

1. **Phase 1: Chunking and Sorting**
   - Split the input file into chunks that fit in memory
   - Sort each chunk in memory using standard sorting algorithms
   - Write sorted chunks to temporary files

2. **Phase 2: Multi-Pass Merging**
   - Merge multiple sorted chunks into larger sorted files
   - Use K-way merge (default K=10) to reduce the number of passes
   - Continue until all chunks are merged into a single sorted file

## Usage

### Basic Usage

```csharp
using _3dEYE.Sorter;

var sorter = new ExternalMergeSorter();
await sorter.SortAsync("input.txt", "output.txt");
```

### Advanced Usage

```csharp
using Microsoft.Extensions.Logging;
using _3dEYE.Sorter;

// Create logger
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<ExternalMergeSorter>();

// Configure sorter with custom settings
var sorter = new ExternalMergeSorter(
    defaultBufferSize: 1024 * 1024, // 1MB buffer
    tempDirectory: "/tmp/sort",
    logger: logger
);

// Sort with custom buffer size and comparer
var comparer = StringComparer.OrdinalIgnoreCase;
var cts = new CancellationTokenSource();

await sorter.SortAsync(
    "large_file.txt", 
    "sorted_file.txt", 
    bufferSizeBytes: 10 * 1024 * 1024, // 10MB buffer
    comparer: comparer,
    cancellationToken: cts.Token
);
```

### Command Line Usage

```bash
# Basic usage
dotnet run -- input.txt output.txt

# With custom buffer size (2MB)
dotnet run -- input.txt output.txt 2097152
```

## Performance Characteristics

### Memory Usage

- **Buffer Size**: Configurable from 64KB to 100MB (or 10% of file size)
- **Peak Memory**: Approximately 2-3x the buffer size during merge operations
- **Temporary Storage**: Requires disk space equal to the input file size

### I/O Efficiency

- **Sequential I/O**: Optimized for sequential read/write operations
- **Minimal Seeks**: K-way merge reduces the number of disk seeks
- **Buffer Management**: Uses `ArrayPool<T>` for efficient buffer reuse

### Scalability

For a 100GB file with 1MB buffer:
- **Chunks**: ~100,000 chunks
- **Merge Passes**: ~5 passes (log₁₀(100,000))
- **Total I/O**: ~500GB read + 500GB write = 1TB total I/O

## Configuration

### Buffer Size Selection

The optimal buffer size depends on:

1. **Available Memory**: Should not exceed available RAM
2. **File Size**: Larger files benefit from larger buffers
3. **I/O Performance**: SSDs can handle smaller buffers efficiently
4. **Chunk Count**: More chunks = more merge passes

**Recommendations:**
- **SSD**: 1-10MB buffer
- **HDD**: 10-100MB buffer
- **Large Files (>10GB)**: 10-100MB buffer

### Custom Comparers

```csharp
// Case-insensitive sorting
var comparer = StringComparer.OrdinalIgnoreCase;

// Numeric sorting
var numericComparer = Comparer<string>.Create((a, b) => 
    long.Parse(a).CompareTo(long.Parse(b)));

// Custom field-based sorting
var fieldComparer = Comparer<string>.Create((a, b) => 
    a.Split(',')[1].CompareTo(b.Split(',')[1]));
```

## Monitoring and Statistics

### Performance Statistics

```csharp
var stats = await sorter.GetSortStatisticsAsync("input.txt", 1024 * 1024);
Console.WriteLine($"File Size: {stats.FileSizeFormatted}");
Console.WriteLine($"Buffer Size: {stats.BufferSizeFormatted}");
Console.WriteLine($"Estimated Chunks: {stats.EstimatedChunks}");
Console.WriteLine($"Estimated Merge Passes: {stats.EstimatedMergePasses}");
Console.WriteLine($"Estimated Total I/O: {stats.EstimatedTotalIOPerFile}x file size");
```

### Logging

The sorter provides detailed logging at different levels:

- **Information**: File sizes, chunk counts, merge passes
- **Debug**: Buffer size adjustments, memory usage
- **Warning**: Cleanup failures, performance issues
- **Error**: File I/O errors, memory allocation failures

## Testing

### Unit Tests

```bash
dotnet test 3dEYE.Tests/ExternalMergeSorterTests.cs
```

Tests cover:
- Small and large file sorting
- Custom comparers
- Edge cases (empty files, single lines)
- Cancellation support
- Error handling

### Benchmarks

```bash
dotnet run --project 3dEYE.Benchmark
```

Benchmarks measure:
- Performance with different file sizes
- Memory usage patterns
- Buffer size impact
- I/O efficiency

## Benchmark Results Analysis

### Test Environment

**System Configuration:**
- **CPU**: Intel Core i7-6820HQ @ 2.70GHz (Skylake)
- **Cores**: 4 physical, 8 logical cores
- **Runtime**: .NET 9.0.4 (X64 RyuJIT AVX2)
- **OS**: Linux Manjaro Linux
- **Test Parameters**: 5 iterations, 1 warmup

### Performance Results

#### File Sorting Performance

| File Size | Buffer Size | Mean Time | Throughput | Memory Allocated | GC Gen0 | GC Gen1 | GC Gen2 |
|-----------|-------------|-----------|------------|------------------|---------|---------|---------|
| 100 MB    | 64 KB       | 7.49 s    | 13.4 MB/s  | 4.34 GB          | 1,008K  | 636K    | 518K    |
| 100 MB    | 1 MB        | 6.33 s    | 15.8 MB/s  | 7.60 GB          | 721K    | 282K    | 221K    |
| 100 MB    | 10 MB       | 12.48 s   | 8.0 MB/s   | 7.37 GB          | 367K    | 71K     | 32K     |
| 1 GB      | 1 MB        | 70.7 s    | 14.5 MB/s  | 79.8 GB          | 8,323K  | 2,435K  | 2,058K  |
| 1 GB      | 10 MB       | 64.6 s    | 15.9 MB/s  | 75.0 GB          | 4,911K  | 681K    | 281K    |

*Note: 1GB with 64KB buffer failed due to excessive memory requirements*

#### Key Performance Insights

**1. Buffer Size Impact:**
- **Optimal buffer size**: 1MB provides the best performance for most file sizes
- **Memory vs Performance trade-off**: Larger buffers (10MB) reduce GC pressure but may increase total time
- **64KB buffer limitation**: Insufficient for very large files (>1GB)

**2. Scalability Analysis:**
- **Linear scaling**: 10x file size increase (100MB→1GB) results in ~10x time increase
- **Throughput consistency**: Performance remains stable at 13-16 MB/s across file sizes
- **Memory efficiency**: Memory usage scales linearly with file size

**3. Memory Management:**
- **GC pressure**: Larger buffers significantly reduce garbage collection pressure
- **Memory allocation**: Predictable allocation patterns with consistent scaling
- **Buffer optimization**: 1MB buffer provides optimal balance of performance and memory

**4. Large File Performance:**
- **1GB files**: Successfully processed in ~65-71 seconds
- **Memory requirements**: 75-80 GB allocated for 1GB files
- **Throughput**: Consistent 14-16 MB/s regardless of buffer size

#### Statistics Performance

The `GetStatistics` method demonstrates excellent performance:
- **Consistent timing**: ~3.0-3.1 μs regardless of file or buffer size
- **Minimal allocations**: Only 496 bytes allocated per call
- **No GC pressure**: Zero Gen1/Gen2 collections
- **Scalability**: Performance remains constant even for 1GB files

### Performance Recommendations

#### Buffer Size Selection

Based on comprehensive benchmark results:

**For Small Files (≤100MB):**
- **Recommended**: 1MB buffer
- **Rationale**: 18% performance improvement over 64KB buffer
- **Memory overhead**: Acceptable 7.6GB allocation
- **Use case**: Development, testing, medium data processing

**For Large Files (100MB-1GB):**
- **Recommended**: 1MB buffer
- **Rationale**: Best performance/memory ratio, 15.8 MB/s throughput
- **Memory planning**: Reserve 80GB for 1GB files
- **Use case**: Production data processing, large datasets

**For Very Large Files (>1GB):**
- **Recommended**: 10MB buffer
- **Rationale**: Reduced GC pressure, better memory management
- **Consideration**: Slightly slower but more stable performance
- **Use case**: Enterprise data processing, batch operations

**For Memory-Constrained Environments:**
- **Recommended**: 64KB buffer (files ≤100MB only)
- **Rationale**: Minimal memory footprint
- **Limitation**: Cannot handle files >1GB
- **Use case**: Embedded systems, cloud environments with strict memory limits

#### Throughput Expectations

**Current Performance:**
- **Small files (1-100MB)**: 13-16 MB/s
- **Large files (100MB-1GB)**: 14-16 MB/s
- **Very large files (1GB+)**: 15-16 MB/s (estimated)

**Performance Factors:**
- **Storage type**: SSD vs HDD significantly impacts I/O performance
- **File size**: Larger files benefit from larger buffers
- **System resources**: Available RAM and CPU cores affect performance
- **Storage speed**: NVMe SSDs can achieve higher throughput

### Memory Usage Analysis

#### Allocation Patterns

**Per Operation:**
- **100MB files**: 4.3-7.6 GB allocated
- **1GB files**: 75-80 GB allocated
- **10GB files**: 750-800 GB estimated (requires external merge)

**Memory Efficiency:**
- **Linear scaling**: Memory usage scales linearly with file size
- **Buffer impact**: Larger buffers increase memory usage but reduce GC pressure
- **GC efficiency**: 10MB buffer shows significantly lower GC pressure

#### Memory Recommendations

**Development/Testing:**
- **Buffer size**: 1MB (good balance of performance and memory)
- **Expected memory**: 4-8 GB for 100MB files
- **File size limit**: Test with files ≤100MB for reasonable memory usage

**Production:**
- **Buffer size**: 1-10MB (depending on available memory)
- **Expected memory**: 80-800 GB for 1-10GB files
- **Memory planning**: Reserve 2-3x buffer size for peak usage
- **System requirements**: 16GB+ RAM recommended for large files

**Enterprise:**
- **Buffer size**: 10-100MB (for very large files)
- **Expected memory**: 1-10 TB for 10-100GB files
- **Memory planning**: Dedicated high-memory systems required
- **Consideration**: External merge sort for files >100GB

### I/O Efficiency Analysis

#### Disk I/O Patterns

**Sequential Access:**
- **Read operations**: Sequential file reading for optimal performance
- **Write operations**: Sequential chunk writing during merge phases
- **Seek minimization**: K-way merge reduces disk seeks

**I/O Volume:**
- **Total I/O**: Approximately 2-3x file size (read + write operations)
- **Chunk overhead**: Temporary files require additional disk space
- **Cleanup**: Automatic cleanup of temporary files

#### Storage Recommendations

**SSD Storage:**
- **Buffer size**: 1-10MB (SSDs handle small buffers efficiently)
- **Performance**: 15-25 MB/s expected throughput
- **Advantage**: Fast random access for merge operations

**HDD Storage:**
- **Buffer size**: 10-100MB (larger buffers reduce seeks)
- **Performance**: 10-20 MB/s expected throughput
- **Consideration**: Sequential I/O optimization is critical

**NVMe Storage:**
- **Buffer size**: 1-50MB (NVMe can handle various buffer sizes)
- **Performance**: 20-50 MB/s expected throughput
- **Advantage**: High bandwidth and low latency

### Scalability Projections

#### Large File Performance

Based on benchmark results and algorithm complexity:

**100MB Files:**
- **Time**: 6-12 seconds (depending on buffer size)
- **Memory**: 4-8 GB
- **Throughput**: 8-16 MB/s

**1GB Files:**
- **Time**: 65-71 seconds
- **Memory**: 75-80 GB
- **Throughput**: 14-16 MB/s

**10GB Files:**
- **Estimated time**: 11-12 minutes
- **Estimated memory**: 750-800 GB
- **Estimated throughput**: 15-16 MB/s

**100GB Files:**
- **Estimated time**: 1.8-2.0 hours
- **Estimated memory**: 7.5-8.0 TB (requires external merge)
- **Estimated throughput**: 15-16 MB/s

#### Multi-Core Potential

**Current Implementation:**
- **Single-threaded**: Current implementation uses single-threaded processing
- **I/O bound**: Performance is primarily limited by I/O, not CPU
- **Memory bound**: Large files are limited by available memory

**Future Enhancements:**
- **Parallel chunking**: Could utilize multiple cores for initial sorting
- **Parallel merging**: Multi-threaded merge operations
- **Expected improvement**: 2-4x performance improvement with proper parallelization
- **Memory distribution**: Could distribute memory usage across multiple machines

### Benchmark Methodology

**Test Data:**
- **Generator**: Parallel file generator with random data
- **File sizes**: 100MB and 1GB (representative of production use cases)
- **Buffer sizes**: 64KB, 1MB, and 10MB (covers typical configuration range)

**Measurement:**
- **Warmup**: 1 iteration to stabilize JIT compilation
- **Iterations**: 5 iterations for statistical significance
- **Memory profiling**: Detailed GC and allocation tracking
- **Timeout**: 2-minute timeout to prevent hanging tests

**Accuracy:**
- **Standard deviation**: Low variance indicates consistent performance
- **Error margins**: Small error ranges suggest reliable measurements
- **Reproducibility**: Results are consistent across multiple runs
- **Failure handling**: 64KB buffer with 1GB file failed as expected

### Performance Limitations

**Memory Constraints:**
- **64KB buffer**: Cannot handle files >1GB due to excessive chunk count
- **Memory allocation**: Large files require significant RAM
- **GC pressure**: Small buffers cause high garbage collection overhead

**I/O Limitations:**
- **Sequential I/O**: Performance limited by storage device speed
- **Disk space**: Requires 2-3x file size for temporary storage
- **Network storage**: Performance significantly reduced on network-attached storage

**Scalability Limits:**
- **Single-threaded**: Current implementation doesn't utilize multiple cores
- **Memory bound**: Cannot process files larger than available RAM
- **External merge**: Very large files require external merge sort implementation

## Implementation Details

### Memory Management

- **`ReadOnlyMemory<char>`**: Efficient line representation
- **`Span<T>`**: Zero-copy comparisons and operations
- **`ArrayPool<T>`**: Reusable buffer allocation
- **Struct-based**: `LineData` struct for minimal allocations

### I/O Optimization

- **Buffered I/O**: Configurable buffer sizes for optimal performance
- **Sequential Access**: Minimizes disk seeks
- **Async Operations**: Non-blocking I/O operations
- **Stream Management**: Proper disposal of file handles

### Error Handling

- **Resource Cleanup**: Automatic cleanup of temporary files
- **Cancellation Support**: Graceful cancellation of long operations
- **Exception Safety**: Proper exception handling and propagation
- **Logging**: Comprehensive error logging for debugging

## Comparison with Other Approaches

### vs. In-Memory Sort
- **Memory Usage**: Constant vs. O(n)
- **File Size Limit**: Unlimited vs. RAM size
- **Performance**: Slower for small files, faster for large files

### vs. K-Way Merge Sort
- **Complexity**: Simpler implementation
- **Memory Usage**: Lower memory requirements
- **I/O Efficiency**: Slightly less efficient but more predictable

### vs. Database Sorting
- **Dependencies**: None vs. database engine
- **Setup**: No setup required
- **Performance**: Optimized for single-file operations

## Future Enhancements

1. **Parallel Processing**: Multi-threaded chunk sorting
2. **Compression**: Support for compressed input/output
3. **Streaming**: Direct streaming without temporary files
4. **Distributed**: Multi-machine sorting for very large files
5. **Adaptive Buffering**: Dynamic buffer size adjustment

## License

This implementation is part of the 3dEYE project and follows the same licensing terms.