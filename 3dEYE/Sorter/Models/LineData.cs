namespace _3dEYE.Sorter.Models;

public readonly struct LineData(ReadOnlyMemory<char> content)
    : IEquatable<LineData>
{
    public readonly ReadOnlyMemory<char> Content = content;

    public static LineData FromString(string line)
    {
        return new LineData(line.AsMemory());
    }

    public string AsString() => Content.ToString();

    public bool Equals(LineData other) => Content.Span.SequenceEqual(other.Content.Span);
    public override bool Equals(object? obj) => obj is LineData other && Equals(other);
    public override int GetHashCode() => Content.Span.GetHashCode();
} 