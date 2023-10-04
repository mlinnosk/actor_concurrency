using ActorSort;
using PowerArgs;
using Proto;
using System.Diagnostics;
using System.Text;

const int _iterations = 20;
const int _arraySize = 10_000_000;
int _poolSize = Environment.ProcessorCount;

Dictionary<string, (Func<CommandArgs, CommandOptions, Task>, CommandOptions)> commands = new ()
{
    { "sort",                   (SortDirectly,   new (_iterations, _arraySize, false, false, false))},
    { "actor_sort",             (SortWithActors, new (_iterations, _arraySize, false, false, false))},
    { "actor_sort_async",       (SortWithActors, new (_iterations, _arraySize, true, false, false))},
    { "actor_sort_split",       (SortWithActors, new (_iterations, _arraySize, false, true, false))},
    { "actor_sort_async_split", (SortWithActors, new (_iterations, _arraySize, true, true, false))},
    
    { "char_pairs",              (TestSingleThreadedCharCount, new (_iterations, _arraySize, false, false, false))},
    { "actor_char_pairs",        (TestCharCountActors,         new (_iterations, _arraySize, false, false, false))},
    { "actor_char_pairs_pooled", (TestCharCountActors,         new (_iterations, _arraySize, false, false, true))},
    { "task_char_pairs",         (TestCharCountWithTasks,      new (_iterations, _arraySize, false, false, false))}
};

var cmdArgs = args.Select(arg => arg.ToLower()).ToList();
CheckArgs(commands, cmdArgs);

Console.WriteLine("Using pool size: {0}.", _poolSize);
Console.WriteLine("Using array size: {0}.", _arraySize);

// Create the system and spawn one root actor.
var system = new ActorSystem();
var sorter = system.Root.Spawn(SorterActor.Props(_poolSize));
var charCounter = system.Root.Spawn(UniqueCharCounterActor.Props(_poolSize));

var rng = new Random();
var data = CreateRandomArray(rng, _arraySize);

await RunTests(commands, new CommandArgs(system, sorter, charCounter, data), cmdArgs);

static void CheckArgs(
    IReadOnlyDictionary<string, (Func<CommandArgs, CommandOptions, Task>, CommandOptions)> commands,
    IEnumerable<string> args)
{
    bool fail = false;

    if (!args.Any())
    {
        fail = true;
    }
    else
    {
        foreach (var command in args)
        {
            if (!commands.ContainsKey(command))
            {
                Console.WriteLine("Unknown command: '{0}'", command);
                fail = true;
            }
        }
    }

    if (fail)
    {
        Console.WriteLine("Valid command:");
        foreach (var command in commands.Keys)
        {
            Console.WriteLine(command);
        }
        Environment.Exit(-1);
    }
}

static async Task RunTests(
    IReadOnlyDictionary<string, (Func<CommandArgs, CommandOptions, Task>, CommandOptions)> commands,
    CommandArgs commandArgs,
    IEnumerable<string> args)
{
    Console.WriteLine("Press any key to start test...");
    Console.ReadKey();
    Console.WriteLine("Running...");

    foreach (var command in args)
    {
        if (commands.TryGetValue(command, out var funcAndOpts))
        {
            await funcAndOpts.Item1(commandArgs, funcAndOpts.Item2);
        }
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
static Task SortDirectly(CommandArgs args, CommandOptions opts)
{
    Console.WriteLine("Sort directly, iterations: {0}", opts.Iterations);

    var sw = Stopwatch.StartNew();

    for (int i = 0; i < opts.Iterations; ++i)
    {
        _ = SorterFuncs.Sort(args.Data);
    }

    var elapsed = sw.ElapsedMilliseconds;

    Console.WriteLine("Sort directly, iterations: {0}, took: {1} ms ({2} ms/iter).\n",
        opts.Iterations, elapsed, elapsed / opts.Iterations);

    return Task.CompletedTask;
}


/// <summary>Sorts the given list using actors</summary>
/// <param name="fullAsync">Dispatch all iterations asynchronously. Otherwise will dispatch one iteration at time.</param>
/// <param name="allowSplit">Allow splitting a single iteration to subranges</param>
static async Task SortWithActors(CommandArgs args, CommandOptions opts)
{
    Console.WriteLine("Sort with actors ({0}, {1})",
        opts.FullAsync ? "full async" : "one at a time",
        opts.AllowSplit ? "use split" : "no split");

    var sw = Stopwatch.StartNew();

    if (opts.FullAsync)
    {
        var tasks = new List<Task>(opts.Iterations);
        for (int i = 0; i < opts.Iterations; ++i)
        {
            tasks.Add(
                args.System.Root.RequestAsync<Sorted>(args.Sorter, new Sort(args.Data, opts.AllowSplit), CancellationToken.None));
        }
        await Task.WhenAll(tasks);
    }
    else
    {
        for (int i = 0; i < opts.Iterations; ++i)
        {
            await args.System.Root.RequestAsync<Sorted>(args.Sorter, new Sort(args.Data, opts.AllowSplit), CancellationToken.None);
        }
    }

    var elapsed = sw.ElapsedMilliseconds;

    Console.WriteLine("Sort with actors ({0}, {1}), took: {2} ms ({3} ms/iter).\n",
        opts.FullAsync ? "full async" : "one at a time",
        opts.AllowSplit ? "use split" : "no split",
        elapsed,
        elapsed / opts.Iterations);
}

static async Task TestIsSorted(CommandArgs args, CommandOptions opts)
{
    if (args.Data.Count != opts.ArraySize)
    {
        Console.WriteLine("Wrong size data, used split: {0}", opts.AllowSplit ? "yes" : "no");
        return;
    }
    var result = await args.System.Root.RequestAsync<Sorted>(args.Sorter, new Sort(args.Data, opts.AllowSplit), CancellationToken.None);
    var isSorted = SorterFuncs.IsSorted(result.SortedData);

    Console.WriteLine("Data sorted: {0}, used split: {1}", isSorted, opts.AllowSplit ? "yes" : "no");
}

/// <summary>Find word pairs from given file which have maximum amount of distinct letters.</summary>
static Task TestSingleThreadedCharCount(CommandArgs args, CommandOptions opts)
{
    Console.WriteLine("Running single threaded char count...");

    // @note takes over 5-10mins
    const long TimeCutOffMs = 20 * 60 * 1000;

    var words = ReadWordsFromFile(args.TestFileName, true);

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

    return Task.CompletedTask;
}

/// <summary>Find word pairs from given file which have maximum amount of distinct letters using actors.</summary>
/// <param name="usePool">Use actor pool instead maximum number amount of actors.</param>
static async Task TestCharCountActors(CommandArgs args, CommandOptions opts)
{
    Console.WriteLine("Running char count with actor...");

    var words = ReadWordsFromFile(args.TestFileName, true);

    Console.WriteLine("Possible combinations: {0}", (long)words.Count * words.Count);

    var sw = Stopwatch.StartNew();

    try
    {
        var result = await args.System.Root.RequestAsync<UniqueChars>(
            args.CharCounter, new CountUniqueChars(words, opts.UsePool), CancellationTokens.FromSeconds(300));

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
static async Task TestCharCountWithTasks(CommandArgs args, CommandOptions opts)
{
    Console.WriteLine("Running char count with tasks...");

    var words = ReadWordsFromFile(args.TestFileName, true);

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

record struct CommandArgs(
    ActorSystem System,
    PID Sorter,
    PID CharCounter,
    IReadOnlyList<int> Data,
    string TestFileName = "les_miserables.txt");

record struct CommandOptions(
    int Iterations,
    int ArraySize,
    bool FullAsync,
    bool AllowSplit,
    bool UsePool);
