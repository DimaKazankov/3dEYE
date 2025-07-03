using System.Runtime.CompilerServices;

namespace _3dEYE.Sorter.Models;

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct MergeEntry(string line, int keyStart, int keyLen, int number, int runIdx)
{
    public string Line { get; }  = line; // full line WITHOUT '\n'
    public int KeyStart { get; } = keyStart; // index where the key starts
    public int KeyLen { get; }  = keyLen; // length (can be 0)
    public int Number { get; }  = number; // numeric prefix
    public int RunIdx { get; }  = runIdx; // origin run

    public ReadOnlySpan<char> KeySpan => Line.AsSpan(KeyStart, KeyLen);
}