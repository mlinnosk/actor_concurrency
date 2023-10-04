using Proto;

namespace ActorSort;

internal record Merge(IReadOnlyList<int> Lhs, IReadOnlyList<int> Rhs);

/// <summary>
/// Actor for mergeing two data sets.
/// </summary>
internal class MergerActor : IActor
{
    public static Props Props() => Proto.Props.FromProducer(() => new MergerActor());

    public Task ReceiveAsync(IContext ctx) => ctx.Message switch
    {
        Merge msg => OnMerge(ctx, msg),
        _ => Task.CompletedTask
    };

    private static Task OnMerge(IContext ctx, Merge msg)
    {
        var merged = SorterFuncs.Merge(msg.Lhs, msg.Rhs);

        ctx.Respond(new Sorted(merged));

        return Task.CompletedTask;
    }
}
