using Proto;

namespace ActorSort;


/// <summary>
/// Actor which can split the given data set into smaller pieces and then sort each of those,
/// then merge the set back.
/// </summary>
internal class SplitterActor : IActor
{
    private readonly PID _sorter;
    private readonly PID _splitter;
    private readonly PID _merger;
    private readonly int _splitMinSize;

    public static Props Props(PID sorter, PID splitter, PID merger, int splitMinSize)
        => Proto.Props.FromProducer(() => new SplitterActor(sorter, splitter, merger, splitMinSize));

    public SplitterActor(PID sorter, PID splitter, PID merger, int splitMinSize)
    {
        _sorter = sorter;
        _splitter = splitter;
        _merger = merger;
        _splitMinSize = splitMinSize;
    }

    public Task ReceiveAsync(IContext ctx) => ctx.Message switch
    {
        Sort msg => OnSort(ctx, msg),
        _ => Task.CompletedTask
    };

    private Task OnSort(IContext ctx, Sort msg)
    {
        if (_splitter is not null && msg.Data.Count > _splitMinSize)
        {
            return SplitAndSort(ctx, msg);
        }
        ctx.Forward(_sorter);

        return Task.CompletedTask;
    }

    private Task SplitAndSort(IContext ctx, Sort msg)
    {
        var (lhs, rhs) = SorterFuncs.Split(msg.Data);

        var tasks = SortSplits(ctx, lhs, rhs);
        ctx.ReenterAfter(Task.WhenAll(tasks), async () =>
        {
            // The tasks are done now, so won't actually wait.
            var sortedLhs = await tasks[0];
            var sortedRhs = await tasks[1];

            // Ctx.Sender is resolved to the original sender in re-enter.
            // Delegate to merger, it handles the response to original request.
            ctx.Request(_merger!, new Merge(sortedLhs.SortedData, sortedRhs.SortedData), ctx.Sender);
        });

        return Task.CompletedTask;
    }

    private Task<Sorted>[] SortSplits(IContext ctx, IReadOnlyList<int> split1, IReadOnlyList<int> split2)
    {
        var tasks = new Task<Sorted>[2];
        tasks[0] = ctx.RequestAsync<Sorted>(_splitter!, new Sort(split1, true), CancellationToken.None);
        tasks[1] = ctx.RequestAsync<Sorted>(_splitter!, new Sort(split2, true), CancellationToken.None);

        return tasks;
    }
}
