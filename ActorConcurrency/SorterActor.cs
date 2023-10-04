using Proto;
using Proto.Router;

namespace ActorSort;

internal record Sort(IReadOnlyList<int> Data, bool AllowSplitting = false);
internal record Sorted(IReadOnlyList<int> SortedData);

/// <summary>
/// Sorts given data.
/// Acts as the suprervisor for the actual workers.
/// </summary>
internal class SorterActor : IActor
{
    readonly int _poolSize;
    PID? _sorterRouter;
    PID? _mergeRouter;
    PID? _splitRouter;

    public static Props Props(int poolSize) => Proto.Props.FromProducer(() => new SorterActor(poolSize));

    public SorterActor(int poolSize)
    {
        _poolSize = poolSize;
    }

    public Task ReceiveAsync(IContext ctx) => ctx.Message switch
    {
        Started => OnStart(ctx),
        Sort msg => OnSort(ctx, msg),
        _ => Task.CompletedTask
    };

    private Task OnStart(IContext ctx)
    {
        _sorterRouter = ctx.Spawn(ctx.NewRoundRobinPool(SortWorker.Props(), _poolSize));
        _mergeRouter = ctx.Spawn(ctx.NewRoundRobinPool(MergerActor.Props(), _poolSize));

        // @note Use self as the "splitter".
        _splitRouter = ctx.Spawn(
            ctx.NewRoundRobinPool(SplitterActor.Props(_sorterRouter!, ctx.Self, _mergeRouter, 1_000_00), _poolSize));

        return Task.CompletedTask;
    }

    private Task OnSort(IContext ctx, Sort msg)
    {
        if (msg.AllowSplitting)
        {
            ctx.Forward(_splitRouter!);
        }
        else
        {
            ctx.Forward(_sorterRouter!);
        }
        return Task.CompletedTask;
    }
}
