using System.Runtime.CompilerServices;

namespace _3dEYE.Sorter.Models;

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
internal readonly struct LineEntry(ReadOnlyMemory<char> full, ReadOnlyMemory<char> key, int number)
    : IComparable<LineEntry>
{
    public ReadOnlyMemory<char> FullLine { get; } = full;
    public ReadOnlyMemory<char> KeyText { get; } = key;
    public int Number { get; } = number;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(LineEntry other)
    {
        var s = KeyText.Span.SequenceCompareTo(other.KeyText.Span);
        return s != 0 ? s : Number.CompareTo(other.Number);
    }
}