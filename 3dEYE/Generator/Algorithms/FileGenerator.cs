using System.Text;
using Microsoft.Extensions.Logging;

namespace _3dEYE.Generator.Algorithms;

public class FileGenerator : IFileGenerator
{
    private readonly ILogger _logger;
    private readonly string[] _input;
    private readonly char[] _lineBuffer;

    private const string Separator = ". ";
    private static readonly string NewLine = Environment.NewLine;

    public FileGenerator(ILogger logger, string[] input)
    {
        if (input.Length == 0)
            throw new ArgumentException("Input strings array cannot be null or empty");
        if (input.Any(string.IsNullOrEmpty))
            throw new ArgumentException("Input strings array contains null or empty strings");
        
        _logger = logger;
        _input = input;
        _lineBuffer = new char[256];
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
            await using var writer = new StreamWriter(filePath, false, Encoding.UTF8, 65536);
            long currentSize = 0;

            _logger.LogInformation("File generation in progress...");

            while (currentSize < fileSizeInBytes)
            {
                var number = Random.Shared.Next(1, 1000000);
                var str = _input[Random.Shared.Next(_input.Length)];
                
                var lineLength = FormatLine(_lineBuffer, number, str);
                var line = new string(_lineBuffer, 0, lineLength);
                
                await writer.WriteLineAsync(line).ConfigureAwait(false);
                currentSize += Encoding.UTF8.GetByteCount(line) + Encoding.UTF8.GetByteCount(NewLine);
            }

            _logger.LogInformation("File generation completed successfully. Final size: {FinalSize} bytes", currentSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during file generation: {FilePath}", filePath);
            throw;
        }
    }

    private static int FormatLine(Span<char> buffer, int number, string str)
    {
        var numberStr = number.ToString();
        
        numberStr.CopyTo(buffer);
        var currentPos = numberStr.Length;
        
        Separator.CopyTo(buffer.Slice(currentPos));
        currentPos += Separator.Length;
        
        str.CopyTo(buffer.Slice(currentPos));
        currentPos += str.Length;
        
        return currentPos;
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