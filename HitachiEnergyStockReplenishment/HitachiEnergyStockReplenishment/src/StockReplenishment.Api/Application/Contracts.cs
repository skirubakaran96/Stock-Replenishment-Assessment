using StockReplenishment.Api.Domain;

namespace StockReplenishment.Api.Application;

public sealed record CreateReplenishmentRequest(string Location, RequestPriority Priority, List<CreateReplenishmentItem> Items);
public sealed record CreateReplenishmentItem(string ArticleNumber, string Description, decimal RequestedQuantity);
public sealed record RejectRequest(string Reason);
public sealed record FulfillRequest(List<FulfilledItem> Items);
public sealed record FulfilledItem(Guid ItemId, decimal FulfilledQuantity);

public sealed record ReplenishmentListItem(
    Guid Id, string Location, RequestPriority Priority, ReplenishmentStatus Status,
    StockValidationStatus StockValidationStatus, int ItemCount, DateTime CreatedAtUtc);

public sealed record ReplenishmentDetail(
    Guid Id, string Location, RequestPriority Priority, ReplenishmentStatus Status,
    StockValidationStatus StockValidationStatus, string? StockValidationMessage, string? RejectionReason,
    DateTime CreatedAtUtc, DateTime? SubmittedAtUtc, DateTime? ApprovedAtUtc, DateTime? FulfilledAtUtc,
    IReadOnlyList<ReplenishmentItemDto> Items);

public sealed record ReplenishmentItemDto(Guid Id, string ArticleNumber, string Description, decimal RequestedQuantity, decimal FulfilledQuantity);
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount, int TotalPages);
public sealed record StockValidationResult(StockValidationStatus Status, string? Message);
