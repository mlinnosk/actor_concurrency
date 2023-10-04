using ActorSort;
using Proto;
using System.Diagnostics;
using System.Text;


const int iterations = 20;
const int arraySize = 10_000_000;
int poolSize = Environment.ProcessorCount;

Console.WriteLine("Using pool size: {0}.", poolSize);
Console.WriteLine("Using array size: {0}.", arraySize);

// Create the system and spawn one root actor.
var system = new ActorSystem();
var sorter = system.Root.Spawn(SorterActor.Props(poolSize));
var charCounter = system.Root.Spawn(UniqueCharCounterActor.Props(poolSize));

var rng = new Random();
var data = CreateRandomArray(rng, arraySize);

await RunTests(iterations, system, sorter, charCounter, data, args);

static async Task RunTests(
    int iterations,
    ActorSystem system,
    PID sorter,
    PID charCounter,
    IReadOnlyList<int> data,
    IEnumerable<string> args)
{
    Console.WriteLine("Press any key to start test...");
    Console.ReadKey();
    Console.WriteLine("Running...");

    if (args.Contains("sort"))
    {
        SortDirectly(data, iterations);       
    }
    if (args.Contains("actor_sort"))
    {
        await SortWithActors(system, sorter, data, iterations, false, false);
    }
    if (args.Contains("actor_sort_async"))
    {
        await SortWithActors(system, sorter, data, iterations, true, false);
    }
    if (args.Contains("actor_sort_split"))
    {
        await SortWithActors(system, sorter, data, iterations, false, true);
    }
    if (args.Contains("actor_sort_async_split"))
    {
        await SortWithActors(system, sorter, data, iterations, true, true);
    }
    if (args.Contains("tests_sorted"))
    {
        await TestIsSorted(system, sorter, data, false);
        await TestIsSorted(system, sorter, data, true);
    }

    if (args.Contains("char_pairs"))
    {
        TestSingleThreadedCharCount("les_miserables.txt");
    }
    if (args.Contains("actor_char_pairs"))
    {
        await TestCharCountActors(system, charCounter, "les_miserables.txt", false);
    }
    if (args.Contains("actor_char_pairs_pooled"))
    {
        await TestCharCountActors(system, charCounter, "les_miserables.txt", true);
    }
    if (args.Contains("task_char_pairs"))
    {
        await TestCharCountWithTasks("les_miserables.txt");
    }

    Console.WriteLine("Running...DONE");
}

static List<int> CreateRandomArray(Random rng, int size)
{
    var data = new List<int>(size);
    for (int i = 0; i < size; ++i)
    {
        data.Add(rng.Next());
    }

    return data;
}

// Sorts the given list using normal single threaded sorting.
static void SortDirectly(IReadOnlyList<int> data, int iterations)
{
    Console.WriteLine("Sort directly, iterations: {0}", iterations);

    var sw = Stopwatch.StartNew();

    for (int i = 0; i < iterations; ++i)
    {
        _ = SorterFuncs.Sort(data);
    }

    var elapsed = sw.ElapsedMilliseconds;

    Console.WriteLine("Sort directly, iterations: {0}, took: {1} ms ({2} ms/iter).\n",
        iterations, elapsed, elapsed / iterations);
}


/// <summary>Sorts the given list using actors</summary>
/// <param name="fullAsync">Dispatch all iterations asynchronously. Otherwise will dispatch one iteration at time.</param>
/// <param name="allowSplit">Allow splitting a single iteration to subranges</param>
static async Task SortWithActors(
    ActorSystem system, PID sorter, IReadOnlyList<int> data, int iterations, bool fullAsync, bool allowSplit)
{
    Console.WriteLine("Sort with actors ({0}, {1})",
        fullAsync ? "full async" : "one at a time",
        allowSplit ? "use split" : "no split");

    var sw = Stopwatch.StartNew();

    if (fullAsync)
    {
        var tasks = new List<Task>(iterations);
        for (int i = 0; i < iterations; ++i)
        {
            tasks.Add(
                system.Root.RequestAsync<Sorted>(sorter, new Sort(data, allowSplit), CancellationToken.None));
        }
        await Task.WhenAll(tasks);
    }
    else
    {
        for (int i = 0; i < iterations; ++i)
        {
            await system.Root.RequestAsync<Sorted>(sorter, new Sort(data, allowSplit), CancellationToken.None);
        }
    }

    var elapsed = sw.ElapsedMilliseconds;

    Console.WriteLine("Sort with actors ({0}, {1}), took: {2} ({3} ms/iter).\n",
        fullAsync ? "full async" : "one at a time",
        allowSplit ? "use split" : "no split",
        elapsed,
        elapsed / iterations);
}

static async Task TestIsSorted(ActorSystem system, PID sorter, IReadOnlyList<int> data, bool allowSplit)
{
    if (data.Count != arraySize)
    {
        Console.WriteLine("Wrong size data, used split: {0}", allowSplit ? "yes" : "no");
        return;
    }
    var result = await system.Root.RequestAsync<Sorted>(sorter, new Sort(data, allowSplit), CancellationToken.None);
    var isSorted = SorterFuncs.IsSorted(result.SortedData);

    Console.WriteLine("Data sorted: {0}, used split: {1}", isSorted, allowSplit ? "yes" : "no");
}

/// <summary>Find word pairs from given file which have maximum amount of distinct letters.</summary>
static void TestSingleThreadedCharCount(string fileName)
{
    Console.WriteLine("Running single threaded char count...");

    // @note takes over 5-10mins
    const long TimeCutOffMs = 20 * 60 * 1000;

    var words = ReadWordsFromFile(fileName, true);

    Console.WriteLine("Possible combinations: {0}", (long)words.Count * words.Count);

    var sw = Stopwatch.StartNew();

    bool didNotFinish = false;
    int completedIterations = 0;
    int maxChars = 0;
    var resultPairs = new List<WordFuncs.ResultPair>(1);

    // O(N^2) algorithm
    foreach (var lhs in words)
    {
        var (count, results) = WordFuncs.CountMaxUniqueChars(lhs, words);
        if (count < maxChars)
        {
            continue;
        }

        if (count > maxChars)
        {
            resultPairs.Clear();
            maxChars = count;
        }

        resultPairs.AddRange(results);
    
        ++completedIterations;

        if (sw.ElapsedMilliseconds > TimeCutOffMs)
        {
            didNotFinish = true;
            break;
        }
    }

    var elapsed = sw.ElapsedMilliseconds;

    if (didNotFinish)
    {
        Console.WriteLine("Did not finish in time, completed: {0} %, {1} iterations, {2} ms/iter",
            (double)completedIterations / words.Count * 100,
            completedIterations,
            elapsed / completedIterations);
        Console.WriteLine("Projected time to completion: {0} s",
            words.Count * (elapsed / completedIterations) / 1000);
    }
    else
    {
        Console.WriteLine("Max char count: {0}, took: {1} s", maxChars,  elapsed / 1000);
        foreach (var (lhs, rhs) in resultPairs)
        {
            Console.WriteLine("Result: {0} : {1}", lhs, rhs);
        }
    }

    Console.WriteLine("Running single threaded char count...DONE");
}

/// <summary>Find word pairs from given file which have maximum amount of distinct letters using actors.</summary>
/// <param name="usePool">Use actor pool instead maximum number amount of actors.</param>
static async Task TestCharCountActors(ActorSystem system, PID charCounter, string fileName, bool usePool)
{
    Console.WriteLine("Running char count with actor...");

    var words = ReadWordsFromFile(fileName, true);

    Console.WriteLine("Possible combinations: {0}", (long)words.Count * words.Count);

    var sw = Stopwatch.StartNew();

    try
    {
        var result = await system.Root.RequestAsync<UniqueChars>(
            charCounter, new CountUniqueChars(words, usePool), CancellationTokens.FromSeconds(300));

        var elapsed = sw.ElapsedMilliseconds;

        Console.WriteLine("Max char count: {0}, took: {1} s", result.Count, elapsed / 1000);
        foreach (var (lhs, rhs) in result.Results)
        {
            Console.WriteLine("Result: {0} : {1}", lhs, rhs);
        }
    }
    catch (TaskCanceledException)
    {
        Console.WriteLine("Failed to complete in time.");
    }
    Console.WriteLine("Running char count with actor...DONE");
}

/// <summary>Find word pairs from given file which have maximum amount of distinct letters using C# task system.</summary>
static async Task TestCharCountWithTasks(string fileName)
{
    Console.WriteLine("Running char count with tasks...");

    var words = ReadWordsFromFile(fileName, true);

    Console.WriteLine("Possible combinations: {0}", (long)words.Count * words.Count);

    var sw = Stopwatch.StartNew();

    try
    {
        var result = await TaskCharCounter.CountAsync(words);

        var elapsed = sw.ElapsedMilliseconds;

        Console.WriteLine("Max char count: {0}, took: {1} s", result.Count, elapsed / 1000);
        foreach (var (lhs, rhs) in result.Results)
        {
            Console.WriteLine("Result: {0} : {1}", lhs, rhs);
        }
    }
    catch (TaskCanceledException)
    {
        Console.WriteLine("Failed to complete in time.");
    }
    Console.WriteLine("Running char count with tasks...DONE");
}

static IReadOnlyList<string> ReadWordsFromFile(string fileName, bool unique)
{
    if (!File.Exists(fileName))
    {
        Console.WriteLine("No file: {0}", fileName);
        throw new Exception(fileName + " not found");
    }

    var sw = Stopwatch.StartNew();

    using var fs = File.OpenRead(fileName);
    var wordsIter = fs.AsWords(Encoding.UTF8).ToLower();
    if (unique)
    {
        wordsIter = wordsIter.Unique();
    }
    var words = wordsIter.ToList();

    var elapsed = sw.ElapsedMilliseconds;

    Console.WriteLine("Read: {0} words, took {1} ms", words.Count, elapsed);

    return words;
}