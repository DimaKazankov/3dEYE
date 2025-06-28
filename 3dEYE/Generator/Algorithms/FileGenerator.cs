using System.Text;
using Microsoft.Extensions.Logging;

namespace _3dEYE.Generator.Algorithms;

public class FileGenerator : IFileGenerator
{
    private readonly ILogger _logger;
    private readonly string[] _input;

    public FileGenerator(ILogger logger, string[] input)
    {
        if (input.Length == 0)
            throw new ArgumentException("Input strings array cannot be null or empty");
        if (input.Any(string.IsNullOrEmpty))
            throw new ArgumentException("Input strings array contains null or empty strings");
        
        _logger = logger;
        _input = input;
    }

    public async Task GenerateFileAsync(string filePath, long fileSizeInBytes)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty");
        if (fileSizeInBytes <= 0)
            throw new ArgumentException($"File size must be greater than 0, but was: {fileSizeInBytes}");

        PrepareDirectory(filePath);
        _logger.LogInformation("Starting file generation process. Target file: {FilePath}, Size: {FileSize} bytes", filePath, fileSizeInBytes);

        try
        {
            var random = new Random();
            await using var writer = new StreamWriter(filePath, false, Encoding.UTF8, 65536);
            long currentSize = 0;

            _logger.LogInformation("File generation in progress...");

            while (currentSize < fileSizeInBytes)
            {
                var number = random.Next(1, 1000000);
                var str = _input[random.Next(_input.Length)];
                var line = $"{number}. {str}";

                await writer.WriteLineAsync(line);
                currentSize += Encoding.UTF8.GetByteCount(line + Environment.NewLine);
            }

            _logger.LogInformation("File generation completed successfully. Final size: {FinalSize} bytes", currentSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during file generation: {FilePath}", filePath);
            throw;
        }
    }

    private void PrepareDirectory(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(directory))
            throw new ArgumentException("File path must include a directory");

        if (Directory.Exists(directory))
            return;

        _logger.LogInformation("Directory doesn't exist. Creating directory: {Directory}", directory);
        Directory.CreateDirectory(directory);
    }
}