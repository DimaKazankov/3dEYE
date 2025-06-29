using System.Text;
using Microsoft.Extensions.Logging;
using _3dEYE.Helpers;

namespace _3dEYE.Generator.Algorithms;

public class FileGenerator : IFileGenerator
{
    private readonly ILogger _logger;
    private readonly string[] _input;
    private readonly ReadOnlyMemory<char>[] _inputMemory;
    private readonly char[] _lineBuffer;

    public FileGenerator(ILogger logger, string[] input)
    {
        if (input.Length == 0)
            throw new ArgumentException("Input strings array cannot be null or empty");
        if (input.Any(string.IsNullOrEmpty))
            throw new ArgumentException("Input strings array contains null or empty strings");
        
        _logger = logger;
        _input = input;
        _inputMemory = FileGeneratorHelpers.ConvertToReadOnlyMemoryArray(input);
        _lineBuffer = new char[256];
    }

    public async Task GenerateFileAsync(string filePath, long fileSizeInBytes)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be null or empty");
        if (fileSizeInBytes <= 0)
            throw new ArgumentException($"File size must be greater than 0, but was: {fileSizeInBytes}");

        FileGeneratorHelpers.PrepareDirectory(filePath, _logger);
        _logger.LogInformation("Starting file generation process. Target file: {FilePath}, Size: {FileSize} bytes", filePath, fileSizeInBytes);

        try
        {
            await using var writer = new StreamWriter(filePath, false, Encoding.UTF8, 65536);
            long currentSize = 0;

            _logger.LogInformation("File generation in progress...");

            while (currentSize < fileSizeInBytes)
            {
                var lineMemory = FileGeneratorHelpers.GenerateRandomLineAsMemory(_inputMemory, _lineBuffer);
                
                await writer.WriteLineAsync(lineMemory).ConfigureAwait(false);
                currentSize += FileGeneratorHelpers.CalculateLineByteCount(lineMemory);
            }

            _logger.LogInformation("File generation completed successfully. Final size: {FinalSize} bytes", currentSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during file generation: {FilePath}", filePath);
            throw;
        }
    }
}