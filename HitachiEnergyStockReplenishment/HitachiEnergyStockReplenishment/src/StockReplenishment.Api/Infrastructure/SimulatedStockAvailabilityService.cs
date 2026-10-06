using StockReplenishment.Api.Application;
using StockReplenishment.Api.Domain;

namespace StockReplenishment.Api.Infrastructure;

public sealed class SimulatedStockAvailabilityService : IStockAvailabilityService
{
    private static readonly Random Random = new();

    public async Task<StockCheckResult> CheckAsync(ReplenishmentRequest request, CancellationToken cancellationToken)
    {
        var delay = Random.Shared.Next(2500, 5001);
        await Task.Delay(delay, cancellationToken);

        // Deterministic outcomes for easy reviewer testing, with enough variability to exercise both paths.
        var available = request.Items.All(i => i.RequestedQuantity <= 500);
        return available
            ? new StockCheckResult(true, $"Stock validation passed after {delay / 1000.0:F1}s. Requested quantities are available.")
            : new StockCheckResult(false, $"Stock validation failed after {delay / 1000.0:F1}s. One or more requested quantities exceed simulated availability.");
    }
}
