using System.Runtime.CompilerServices;

namespace _3dEYE.Sorter;

/// <summary>Queue element used during k‑way merge.</summary>
[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
readonly struct MergeEntry(string line, int keyStart, int keyLen, int number, int runIdx)
{
    public readonly string Line = line; // full line WITHOUT '\n'
    public readonly int KeyStart = keyStart; // index where the key starts
    public readonly int KeyLen = keyLen; // length (can be 0)
    public readonly int Number = number; // numeric prefix
    public readonly int RunIdx = runIdx; // origin run

    public ReadOnlySpan<char> KeySpan => Line.AsSpan(KeyStart, KeyLen);
}