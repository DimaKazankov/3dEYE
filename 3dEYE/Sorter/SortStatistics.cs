namespace _3dEYE.Sorter;

public class SortStatistics
{
    public long FileSizeBytes { get; set; }
    public int BufferSizeBytes { get; set; }
    public int EstimatedChunks { get; set; }
    public int EstimatedMergePasses { get; set; }
    public int EstimatedTotalIOPerFile { get; set; }
    
    public string FileSizeFormatted => FormatBytes(FileSizeBytes);
    public string BufferSizeFormatted => FormatBytes(BufferSizeBytes);
    
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        var order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}