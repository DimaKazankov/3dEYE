using System.Text;
using Microsoft.Extensions.Logging;

namespace _3dEYE.Helpers;

public static class FileGeneratorHelpers
{
    private const string Separator = ". ";
    private static readonly string NewLine = Environment.NewLine;
    
    public static int FormatLine(Span<char> buffer, int number, string str)
    {
        var numberStr = number.ToString();
        
        // Copy number
        numberStr.CopyTo(buffer);
        var currentPos = numberStr.Length;
        
        // Copy separator
        Separator.CopyTo(buffer.Slice(currentPos));
        currentPos += Separator.Length;
        
        // Copy string
        str.CopyTo(buffer.Slice(currentPos));
        currentPos += str.Length;
        
        return currentPos;
    }

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

    public static ReadOnlyMemory<char> FormatLineToMemory(char[] buffer, int number, ReadOnlyMemory<char> str)
    {
        var lineLength = FormatLine(buffer, number, str);
        return new ReadOnlyMemory<char>(buffer, 0, lineLength);
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
    
    public static int CalculateLineByteCount(string line)
    {
        return Encoding.UTF8.GetByteCount(line) + Encoding.UTF8.GetByteCount(NewLine);
    }

    public static int CalculateLineByteCount(ReadOnlyMemory<char> line)
    {
        return Encoding.UTF8.GetByteCount(line.Span) + Encoding.UTF8.GetByteCount(NewLine);
    }

    public static string GenerateRandomLine(string[] input, char[] lineBuffer, long globalOffset = 0)
    {
        var number = Random.Shared.Next(1, 1000000) + (int)(globalOffset / 50);
        var str = input[Random.Shared.Next(input.Length)];
        
        var lineLength = FormatLine(lineBuffer, number, str);
        return new string(lineBuffer, 0, lineLength);
    }

    public static ReadOnlyMemory<char> GenerateRandomLineAsMemory(string[] input, char[] lineBuffer, long globalOffset = 0)
    {
        var number = Random.Shared.Next(1, 1000000) + (int)(globalOffset / 50);
        var str = input[Random.Shared.Next(input.Length)];
        
        var lineLength = FormatLine(lineBuffer, number, str);
        return new ReadOnlyMemory<char>(lineBuffer, 0, lineLength);
    }

    public static ReadOnlyMemory<char> GenerateRandomLineAsMemory(ReadOnlyMemory<char>[] input, char[] lineBuffer, long globalOffset = 0)
    {
        var number = Random.Shared.Next(1, 1000000) + (int)(globalOffset / 50);
        var str = input[Random.Shared.Next(input.Length)];
        
        var lineLength = FormatLine(lineBuffer, number, str);
        return new ReadOnlyMemory<char>(lineBuffer, 0, lineLength);
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