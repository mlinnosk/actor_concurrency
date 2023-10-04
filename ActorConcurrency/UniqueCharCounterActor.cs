using Proto;
using Proto.Router;

namespace ActorSort;

internal record CountUniqueChars(IReadOnlyList<string> Words, bool UsePool = false);

internal record UniqueChars(int Count, IReadOnlyList<WordFuncs.ResultPair> Results);

internal record struct JobId(long Value)
{
    public override readonly string ToString() => $"job_{Value}";
}

internal record UniqueCharsInternal(JobId JobId, int Count, IReadOnlyList<WordFuncs.ResultPair> Results);

/// <summary>
/// A supervisor actor to count unique chars in all possible word pairs.
/// Has a worker pool.
/// </summary>
internal class UniqueCharCounterActor : IActor
{
    private record struct JobState(
        PID Client, 
        int Expected, 
        int Current, 
        int CurrentMax, 
        List<WordFuncs.ResultPair> CurrentResults);
    
    private readonly Dictionary<JobId, JobState> _jobs = new ();
    private readonly int _poolSize;
    private readonly JobIdSource _idSource = new ();
    private PID? _pool;

    public static Props Props(int poolSize) => Proto.Props.FromProducer(() =>  new UniqueCharCounterActor(poolSize));

    public UniqueCharCounterActor(int poolSize) => _poolSize = poolSize;

    public Task ReceiveAsync(IContext ctx) => ctx.Message switch
    {
        Started => OnStart(ctx),
        CountUniqueChars msg => OnCountUniqueChars(ctx, msg),
        UniqueCharsInternal msg => OnUniqueChars(ctx, msg),
        _ => Task.CompletedTask
    };

    private Task OnStart(IContext ctx)
    {
        _pool = ctx.Spawn(ctx.NewRoundRobinPool(CounterWorker.Props(false), _poolSize));
        return Task.CompletedTask;
    }

    private Task OnCountUniqueChars(IContext ctx, CountUniqueChars msg)
    {
        var jobId = DispatchJob(ctx, _idSource, msg, _pool!);
        _jobs[jobId] = new JobState(ctx.Sender!, msg.Words.Count, 0, 0, new List<WordFuncs.ResultPair>());

        Console.WriteLine("Dispatched all work for id: {0}", jobId);

        return Task.CompletedTask;
    }

    private static JobId DispatchJob(IContext ctx, JobIdSource idSource, CountUniqueChars msg, PID pool)
    {
        var jobId = idSource.Next();
        foreach (var word in msg.Words)
        {
            ctx.Request(GetWorker(ctx, pool, msg.UsePool), new CountUniqueCharsForSingleWord(jobId, word, msg.Words));
        };

        return jobId;
    }

    private static PID GetWorker(IContext ctx, PID pool, bool usePool)
        => usePool ? pool : ctx.Spawn(CounterWorker.Props());

    private Task OnUniqueChars(IContext ctx, UniqueCharsInternal msg)
    {
        if (!_jobs.TryGetValue(msg.JobId, out var jobData))
        {
            throw new Exception($"No job: {msg.JobId}");
        }

        MergeResults(msg, in jobData);

        int current = jobData.Current + 1;
        _jobs[msg.JobId] = jobData with
        {
            Current = current,
            CurrentMax = Math.Max(jobData.CurrentMax, msg.Count),
        };

        return Done(ctx, msg, in jobData, current);

        static void MergeResults(UniqueCharsInternal msg, in JobState jobData)
        {
            if (msg.Count >= jobData.CurrentMax)
            {
                if (msg.Count > jobData.CurrentMax)
                {
                    jobData.CurrentResults.Clear();
                }

                jobData.CurrentResults.AddRange(msg.Results);
            }
        }

        Task Done(IContext ctx, UniqueCharsInternal msg, in JobState jobData, int current)
        {
            if (current == jobData.Expected)
            {
                return SendResult(ctx, msg.Count, msg.JobId, jobData.Client, jobData.CurrentMax, jobData.CurrentResults);
            }

            return Task.CompletedTask;
        }
    }

    private Task SendResult(IContext ctx, int newCount, JobId jobId, PID client, int currentMax, IReadOnlyList<WordFuncs.ResultPair> results)
    {
        int max = Math.Max(newCount, currentMax);

        ctx.Send(client, new UniqueChars(max, results));

        _jobs.Remove(jobId);

        Console.WriteLine("{0} job completed", jobId);

        return Task.CompletedTask;
    }
}

internal class JobIdSource
{
    private long _current;

    public JobId Next() => new (_current++);
}