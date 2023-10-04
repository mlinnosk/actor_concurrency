using Proto;

namespace ActorSort;

internal record CountUniqueCharsForSingleWord(JobId JobId, string Ref, IReadOnlyList<string> Words);

/// <summary>
/// Actor for counting unique chars in word pairs.
/// </summary>
internal class CounterWorker : IActor
{
    private readonly bool _shutdownWhenDone;

    public static Props Props(bool shutDownWhenDone = true)
        => Proto.Props.FromProducer(() =>  new CounterWorker(shutDownWhenDone));

    public CounterWorker(bool shutdownWhenDone) => _shutdownWhenDone = shutdownWhenDone;

    public Task ReceiveAsync(IContext ctx) => ctx.Message switch
    {
        CountUniqueCharsForSingleWord msg => OnCountUniqueCharsForSingleWord(ctx, msg),
        _ => Task.CompletedTask
    };

    private Task OnCountUniqueCharsForSingleWord(IContext ctx, CountUniqueCharsForSingleWord msg)
    {
        var (maxCount, result) = WordFuncs.CountMaxUniqueChars(msg.Ref, msg.Words);

        ctx.Respond(new UniqueCharsInternal(msg.JobId, maxCount, result));

        if (_shutdownWhenDone)
        {
            // Stop as we are now done.
            ctx.Stop(ctx.Self);
        }

        return Task.CompletedTask;
    }
}