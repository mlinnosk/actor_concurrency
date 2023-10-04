namespace ActorSort;

internal static class TaskCharCounter
{
    internal static async Task<UniqueChars> CountAsync(IReadOnlyList<string> words)
        => MergeResults(await Task.WhenAll(SpawnTasks(words)).ConfigureAwait(false));

    private static IReadOnlyList<Task<UniqueChars>> SpawnTasks(IReadOnlyList<string> words)
    {
        var tasks = new List<Task<UniqueChars>>(words.Count);
        tasks.AddRange(words.Select(word => CountForWordAsync(word, words)));

        return tasks;
    }

    private static Task<UniqueChars> CountForWordAsync(string word, IReadOnlyList<string> words)
    => Task.Run(() =>
    {
        var (maxCount, result) = WordFuncs.CountMaxUniqueChars(word, words);
        return Task.FromResult(new UniqueChars(maxCount, result));
    });

    private static UniqueChars MergeResults(IEnumerable<UniqueChars> results)
    {
        int currentMax = 0; 
        var currentResults = new List<WordFuncs.ResultPair>();
        
        foreach (var result in results)
        {
            if (result.Count >= currentMax)
            {
                if (result.Count > currentMax)
                {
                    currentResults.Clear();
                }

                currentResults.AddRange(result.Results);
                currentMax = result.Count;
            }
        }

        return new UniqueChars(currentMax, currentResults);
    }
}