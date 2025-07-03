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
} 