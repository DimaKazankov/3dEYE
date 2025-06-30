using System.Globalization;

namespace _3dEYE.Sorter.Models;

public class LineDataComparer : IComparer<LineData>
{
    public int Compare(LineData x, LineData y)
    {
        var spanX = x.Content.Span;
        var spanY = y.Content.Span;
        
        // Parse both lines to extract number and string parts
        var (numberX, stringStartX, stringLengthX) = ParseLine(spanX);
        var (numberY, stringStartY, stringLengthY) = ParseLine(spanY);
        
        // Get string spans for comparison
        var stringSpanX = spanX.Slice(stringStartX, stringLengthX);
        var stringSpanY = spanY.Slice(stringStartY, stringLengthY);
        
        // First, compare by string part (alphabetically) using span comparison
        var stringComparison = stringSpanX.SequenceCompareTo(stringSpanY);
        
        // If strings are equal, compare by number (ascending)
        return stringComparison == 0 ? numberX.CompareTo(numberY) : stringComparison;
    }
    
    private static (int number, int stringStart, int stringLength) ParseLine(ReadOnlySpan<char> lineSpan)
    {
        if (lineSpan.IsEmpty)
            return (0, 0, 0);
        
        // Find the first occurrence of ". "
        var separatorIndex = lineSpan.IndexOf(". ");
        if (separatorIndex == -1)
        {
            // If no separator found, treat entire line as string with number 0
            return (0, 0, lineSpan.Length);
        }
        
        // Extract number part (before the separator)
        var numberSpan = lineSpan[..separatorIndex].Trim();
        
        // Calculate string part position and length
        var stringStart = separatorIndex + 2; // +2 for ". "
        var stringLength = lineSpan.Length - stringStart;

        // Try to parse the number part
        return int.TryParse(numberSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? (number, stringStart, stringLength) :
            // If number parsing fails, treat entire line as string with number 0
            (0, 0, lineSpan.Length);
    }
} 