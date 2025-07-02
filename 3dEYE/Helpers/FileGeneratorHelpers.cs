using System.Text;
using Microsoft.Extensions.Logging;

namespace _3dEYE.Helpers;

public static class FileGeneratorHelpers
{
    private const string Separator = ". ";
    private static readonly string NewLine = Environment.NewLine;

    public static int FormatLine(Span<char> buffer, int number, ReadOnlyMemory<char> str)
    {
        var numberStr = number.ToString();
        
        // Copy number
        numberStr.CopyTo(buffer);
        var currentPos = numberStr.Length;
        
        // Copy separator
        Separator.CopyTo(buffer.Slice(currentPos));
        currentPos += Separator.Length;
        
        // Copy string using ReadOnlyMemory<char>
        str.Span.CopyTo(buffer.Slice(currentPos));
        currentPos += str.Length;
        
        return currentPos;
    }

    public static void PrepareDirectory(string filePath, ILogger logger)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(directory))
            throw new ArgumentException("File path must include a directory");

        if (Directory.Exists(directory))
            return;

        logger.LogInformation("Directory doesn't exist. Creating directory: {Directory}", directory);
        Directory.CreateDirectory(directory);
    }

    public static int CalculateLineByteCount(ReadOnlyMemory<char> line)
    {
        return Encoding.UTF8.GetByteCount(line.Span) + Encoding.UTF8.GetByteCount(NewLine);
    }

    public static ReadOnlyMemory<char> GenerateRandomLineAsMemory(ReadOnlyMemory<char>[] input, char[] lineBuffer, long globalOffset = 0)
    {
        var number = Random.Shared.Next(1, 1000000) + (int)(globalOffset / 50);
        
        var lineLength = FormatLine(lineBuffer, number, input[Random.Shared.Next(input.Length)]);
        // Create a copy of the buffer content to prevent buffer reuse issues
        var bufferCopy = new char[lineLength];
        Array.Copy(lineBuffer, 0, bufferCopy, 0, lineLength);
        return new ReadOnlyMemory<char>(bufferCopy);
    }

    public static ReadOnlyMemory<char>[] ConvertToReadOnlyMemoryArray(string[] input)
    {
        var result = new ReadOnlyMemory<char>[input.Length];
        for (var i = 0; i < input.Length; i++)
        {
            result[i] = input[i].AsMemory();
        }
        return result;
    }
} 