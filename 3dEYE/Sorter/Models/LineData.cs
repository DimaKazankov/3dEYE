namespace _3dEYE.Sorter.Models;

public readonly struct LineData(ReadOnlyMemory<char> content, long originalPosition)
    : IComparable<LineData>, IEquatable<LineData>
{
    public readonly ReadOnlyMemory<char> Content = content;
    
    public readonly long OriginalPosition = originalPosition;
    
    public readonly int Length = content.Length;

    public static LineData FromString(string line, long originalPosition)
    {
        return new LineData(line.AsMemory(), originalPosition);
    }

    public static LineData FromSpan(ReadOnlySpan<char> line, long originalPosition)
    {
        return new LineData(line.ToArray().AsMemory(), originalPosition);
    }

    public string AsString() => Content.ToString();

    public ReadOnlySpan<char> AsSpan() => Content.Span;

    public int CompareTo(LineData other)
    {
        return Content.Span.SequenceCompareTo(other.Content.Span);
    }

    public bool Equals(LineData other)
    {
        return Content.Span.SequenceEqual(other.Content.Span);
    }

    public override bool Equals(object? obj)
    {
        return obj is LineData other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Content.Span.GetHashCode();
    }

    public static bool operator ==(LineData left, LineData right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LineData left, LineData right)
    {
        return !left.Equals(right);
    }

    public static bool operator <(LineData left, LineData right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(LineData left, LineData right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >(LineData left, LineData right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator >=(LineData left, LineData right)
    {
        return left.CompareTo(right) >= 0;
    }
} 