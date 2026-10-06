using Microsoft.EntityFrameworkCore;
using StockReplenishment.Api.Background;
using StockReplenishment.Api.Domain;
using StockReplenishment.Api.Infrastructure;

namespace StockReplenishment.Api.Application;

public sealed class ReplenishmentRequestService(AppDbContext db, IStockValidationQueue validationQueue) : IReplenishmentRequestService
{
    public async Task<PagedResult<ReplenishmentListItem>> GetAsync(ReplenishmentStatus? status, RequestPriority? priority, string? location, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.ReplenishmentRequests.AsNoTracking().AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (priority.HasValue) query = query.Where(x => x.Priority == priority.Value);
        if (!string.IsNullOrWhiteSpace(location)) query = query.Where(x => x.Location.Contains(location.Trim()));

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAtUtc)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(x => new ReplenishmentListItem(x.Id, x.Location, x.Priority, x.Status, x.StockValidationStatus, x.Items.Count, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
        return new PagedResult<ReplenishmentListItem>(items, pageNumber, pageSize, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<ReplenishmentDetail?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.ReplenishmentRequests.AsNoTracking().Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<ReplenishmentDetail> CreateAsync(CreateReplenishmentRequest request, CancellationToken cancellationToken)
    {
        ValidateCreateRequest(request);
        var entity = ReplenishmentRequest.Create(request.Location, request.Priority,
            request.Items.Select(i => ReplenishmentItem.Create(i.ArticleNumber, i.Description, i.RequestedQuantity)));
        db.ReplenishmentRequests.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task SubmitAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await GetTracked(id, cancellationToken);
        entity.Submit();
        await db.SaveChangesAsync(cancellationToken);
        await validationQueue.EnqueueAsync(id, CancellationToken.None);
    }

    public async Task ApproveAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await GetTracked(id, cancellationToken);
        entity.Approve();
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(Guid id, string reason, CancellationToken cancellationToken)
    {
        var entity = await GetTracked(id, cancellationToken);
        entity.Reject(reason);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task FulfillAsync(Guid id, IReadOnlyDictionary<Guid, decimal> fulfilledQuantities, CancellationToken cancellationToken)
    {
        var entity = await GetTracked(id, cancellationToken);
        entity.Fulfill(fulfilledQuantities);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<StockValidationResult?> GetStockValidationAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.ReplenishmentRequests.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : new StockValidationResult(entity.StockValidationStatus, entity.StockValidationMessage);
    }

    private async Task<ReplenishmentRequest> GetTracked(Guid id, CancellationToken cancellationToken)
        => await db.ReplenishmentRequests.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
           ?? throw new NotFoundException($"Replenishment request '{id}' was not found.");

    private static void ValidateCreateRequest(CreateReplenishmentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Location)) throw new ValidationException("Location is required.");
        if (request.Location.Length > 100) throw new ValidationException("Location cannot exceed 100 characters.");
        if (request.Items is null || request.Items.Count == 0) throw new ValidationException("At least one material item is required.");
        if (request.Items.Any(i => string.IsNullOrWhiteSpace(i.ArticleNumber) || string.IsNullOrWhiteSpace(i.Description)))
            throw new ValidationException("Article number and description are required for every item.");
        if (request.Items.Any(i => i.RequestedQuantity <= 0)) throw new ValidationException("Requested quantity must be greater than zero.");
    }

    private static ReplenishmentDetail Map(ReplenishmentRequest x) => new(
        x.Id, x.Location, x.Priority, x.Status, x.StockValidationStatus, x.StockValidationMessage, x.RejectionReason,
        x.CreatedAtUtc, x.SubmittedAtUtc, x.ApprovedAtUtc, x.FulfilledAtUtc,
        x.Items.Select(i => new ReplenishmentItemDto(i.Id, i.ArticleNumber, i.Description, i.RequestedQuantity, i.FulfilledQuantity)).ToList());
}

public sealed class NotFoundException(string message) : Exception(message);
public sealed class ValidationException(string message) : Exception(message);
