using System.Globalization;

namespace _3dEYE.Sorter.Models;

// todo. to work on edge cases
public class LineDataComparer : IComparer<LineData>
{
    public int Compare(LineData x, LineData y)
    {
        var spanX = x.Content.Span;
        var spanY = y.Content.Span;
        
        var (numberX, stringStartX, stringLengthX) = ParseLine(spanX);
        var (numberY, stringStartY, stringLengthY) = ParseLine(spanY);
        
        var stringSpanX = spanX.Slice(stringStartX, stringLengthX);
        var stringSpanY = spanY.Slice(stringStartY, stringLengthY);
        
        var stringComparison = stringSpanX.SequenceCompareTo(stringSpanY);
        return stringComparison == 0 ? numberX.CompareTo(numberY) : stringComparison;
    }
    
    private static (int number, int stringStart, int stringLength) ParseLine(ReadOnlySpan<char> lineSpan)
    {
        if (lineSpan.IsEmpty)
            return (0, 0, 0);
        
        var separatorIndex = lineSpan.IndexOf(". ");
        if (separatorIndex == -1)
            return (0, 0, lineSpan.Length);
        
        var numberSpan = lineSpan[..separatorIndex].Trim();
        
        var stringStart = separatorIndex + 2; // +2 for ". "
        var stringLength = lineSpan.Length - stringStart;

        return int.TryParse(numberSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)
            ? (number, stringStart, stringLength) : (0, 0, lineSpan.Length);
    }
} 