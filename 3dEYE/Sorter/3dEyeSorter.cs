using _3dEYE.Sorter.Models;
using _3dEYE.Sorter.Utils;

namespace _3dEYE.Sorter;

public class ThreeDEyeSorter
{
    public async Task SortAsync(string input, string output, string temp)
    {
        var comparer = new MergeEntryComparer();
        
        var merger = new KWaySortMerger(comparer);
        var processor = new FileChunkProcessor();
        
        var chunks = await processor.SplitToChunksAsync(input, temp);
        await merger.MergeAsync(chunks, output);
    }
}