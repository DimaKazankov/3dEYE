using System.Runtime.CompilerServices;

namespace _3dEYE.Sorter.Models;

public sealed class MergeEntryComparer : IComparer<MergeEntry>
{
    public static readonly MergeEntryComparer Instance = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Compare(MergeEntry x, MergeEntry y)
    {
        var s = x.KeySpan.SequenceCompareTo(y.KeySpan);
        return s != 0 ? s : x.Number.CompareTo(y.Number);
    }
}