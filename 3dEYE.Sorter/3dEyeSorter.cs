namespace _3dEYE.Sorter;

public class ThreeDEyeSorter
{
    public async Task SortAsync(string input, string output, string temp)
    {
        var runFiles = await SplitSortWorker.SplitIntoRunsAsync(input, temp);
        await KWayMergeRunner.MergeRunsAsync(runFiles, output);
    }
}