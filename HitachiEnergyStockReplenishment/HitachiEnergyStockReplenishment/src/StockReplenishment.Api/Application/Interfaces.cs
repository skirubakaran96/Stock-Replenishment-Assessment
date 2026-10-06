using StockReplenishment.Api.Domain;

namespace StockReplenishment.Api.Application;

public interface IReplenishmentRequestService
{
    Task<PagedResult<ReplenishmentListItem>> GetAsync(ReplenishmentStatus? status, RequestPriority? priority, string? location, int pageNumber, int pageSize, CancellationToken cancellationToken);
    Task<ReplenishmentDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ReplenishmentDetail> CreateAsync(CreateReplenishmentRequest request, CancellationToken cancellationToken);
    Task SubmitAsync(Guid id, CancellationToken cancellationToken);
    Task ApproveAsync(Guid id, CancellationToken cancellationToken);
    Task RejectAsync(Guid id, string reason, CancellationToken cancellationToken);
    Task FulfillAsync(Guid id, IReadOnlyDictionary<Guid, decimal> fulfilledQuantities, CancellationToken cancellationToken);
    Task<StockValidationResult?> GetStockValidationAsync(Guid id, CancellationToken cancellationToken);
}

public interface IStockAvailabilityService
{
    Task<StockCheckResult> CheckAsync(ReplenishmentRequest request, CancellationToken cancellationToken);
}

public sealed record StockCheckResult(bool IsAvailable, string Message);
