using System.Runtime.CompilerServices;

namespace _3dEYE.Sorter;

/// <summary>Immutable view over one input line during the split phase.</summary>
[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
internal readonly struct LineEntry(ReadOnlyMemory<char> full, ReadOnlyMemory<char> key, int number)
    : IComparable<LineEntry>
{
    public readonly ReadOnlyMemory<char> FullLine = full; // "<num>. <text>\n"
    public readonly ReadOnlyMemory<char> KeyText = key;  // "<text>"
    public readonly int Number = number;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(LineEntry other)
    {
        var s = KeyText.Span.SequenceCompareTo(other.KeyText.Span);
        return s != 0 ? s : Number.CompareTo(other.Number);
    }
}