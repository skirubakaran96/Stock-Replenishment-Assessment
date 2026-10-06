using System.Threading.Channels;

namespace StockReplenishment.Api.Background;

public interface IStockValidationQueue
{
    ValueTask EnqueueAsync(Guid requestId, CancellationToken cancellationToken);
    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);
}

public sealed class StockValidationQueue : IStockValidationQueue
{
    private readonly Channel<Guid> _queue = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public ValueTask EnqueueAsync(Guid requestId, CancellationToken cancellationToken) => _queue.Writer.WriteAsync(requestId, cancellationToken);
    public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken) => _queue.Reader.ReadAsync(cancellationToken);
}
