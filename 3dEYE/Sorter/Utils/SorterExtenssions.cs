using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using _3dEYE.Sorter.Models;

namespace _3dEYE.Sorter.Utils;

internal static class SorterExtensions
{
    private static readonly Decoder Utf8Decoder = Encoding.UTF8.GetDecoder();

    public static async Task<int> FillBufferUtf8Async(this PipeReader reader, Memory<char> dest)
    {
        var written = 0;
        while (written < dest.Length)
        {
            var result = await reader.ReadAsync();
            var seq = result.Buffer;
            if (seq.IsEmpty && result.IsCompleted) break;

            var consumed = seq.Start;
            while (seq.Length > 0 && written < dest.Length)
            {
                var bytes = seq.FirstSpan;
                var chars = dest.Span.Slice(written);

                Utf8Decoder.Convert(bytes, chars, flush: result.IsCompleted, out var bytesUsed, out var charsUsed, out _);

                written += charsUsed;
                consumed = seq.GetPosition(bytesUsed, consumed);
                seq = seq.Slice(consumed);
            }

            reader.AdvanceTo(consumed);
            if (result.IsCompleted || written == dest.Length) break;
        }

        return written;
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LastLf(this Span<char> span)
    {
        var i = span.LastIndexOf('\n');
        if (i < 0) throw new InvalidDataException("Input line longer than the configured maxLineChars");
        return i + 1;
    }
    
    public static async Task FlushRunAsync(this IEnumerable<LineEntry> entries, string path)
    {
        await using var w = new StreamWriter(path, append: false, Encoding.UTF8, bufferSize: 1 << 20);
        foreach (var e in entries) await w.WriteAsync(e.FullLine);
    }
}