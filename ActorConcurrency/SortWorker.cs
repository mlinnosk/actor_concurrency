using Proto;

namespace ActorSort;

/// <summary>
/// Simply sorts the given data.
/// </summary>
internal class SortWorker : IActor
{
    public static Props Props() => Proto.Props.FromProducer(() => new SortWorker());

    public Task ReceiveAsync(IContext context)
    {
        if (context.Message is Sort msg)
        {
            OnSort(context, msg);
        }

        return Task.CompletedTask;
    }

    private static Task OnSort(IContext ctx, Sort msg)
    {
        var copy = new List<int>(msg.Data);
        copy.Sort();
        ctx.Respond(new Sorted(copy));

        return Task.CompletedTask;
    }
}
