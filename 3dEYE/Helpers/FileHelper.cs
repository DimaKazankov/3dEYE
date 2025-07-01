using Microsoft.Extensions.Logging;
using System.Text;

namespace _3dEYE.Helpers;

public static class FileHelper
{
    public static async Task WithCleanup(Func<string, Task> action, string fileName, ILogger? logger = null)
    {
        var filePath = Path.Combine(Path.GetTempPath(), fileName);
        await action(filePath);
        if (!File.Exists(filePath)) return;
        try
        {
            File.Delete(filePath);
        }
        catch (Exception ex)
        {
            logger?.LogError("Warning: Could not delete temporary file {FilePath}: {Message}", filePath, ex.Message);
        }
    }

    public static async Task<T> WithCleanup<T>(Func<string, Task<T>> action, string fileName, ILogger? logger = null)
    {
        var filePath = Path.Combine(Path.GetTempPath(), fileName);
        var result = await action(filePath);
        if (!File.Exists(filePath)) return result;
        try
        {
            File.Delete(filePath);
        }
        catch (Exception ex)
        {
            logger?.LogError("Warning: Could not delete temporary file {FilePath}: {Message}", filePath, ex.Message);
        }
        return result;
    }
    
    public static async Task CopyFileAsync(
        string sourceFilePath,
        string destinationFilePath,
        int bufferSize = 1024 * 1024, // Default 1MB buffer
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourceFilePath))
            throw new FileNotFoundException($"Source file not found: {sourceFilePath}");

        using var reader = new StreamReader(sourceFilePath, Encoding.UTF8, true, bufferSize);
        await using var writer = new StreamWriter(destinationFilePath, false, Encoding.UTF8, bufferSize);
        
        var buffer = new char[bufferSize];
        int bytesRead;
        
        while ((bytesRead = await reader.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
        }
    }
} 