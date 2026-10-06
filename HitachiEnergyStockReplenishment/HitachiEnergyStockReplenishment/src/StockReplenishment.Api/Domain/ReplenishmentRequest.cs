namespace StockReplenishment.Api.Domain;

public sealed class ReplenishmentRequest
{
    private ReplenishmentRequest() { }

    public Guid Id { get; private set; }
    public string Location { get; private set; } = string.Empty;
    public RequestPriority Priority { get; private set; }
    public ReplenishmentStatus Status { get; private set; }
    public StockValidationStatus StockValidationStatus { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? StockValidationMessage { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public DateTime? SubmittedAtUtc { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public DateTime? FulfilledAtUtc { get; private set; }
    public ICollection<ReplenishmentItem> Items { get; private set; } = new List<ReplenishmentItem>();

    public static ReplenishmentRequest Create(string location, RequestPriority priority, IEnumerable<ReplenishmentItem> items)
    {
        if (string.IsNullOrWhiteSpace(location)) throw new DomainException("Location is required.");
        var itemList = items.ToList();
        if (itemList.Count == 0) throw new DomainException("At least one material item is required.");

        var now = DateTime.UtcNow;
        return new ReplenishmentRequest
        {
            Id = Guid.NewGuid(), Location = location.Trim(), Priority = priority,
            Status = ReplenishmentStatus.Draft, StockValidationStatus = StockValidationStatus.NotStarted,
            CreatedAtUtc = now, UpdatedAtUtc = now, Items = itemList
        };
    }

    public void Submit()
    {
        EnsureStatus(ReplenishmentStatus.Draft, "Only draft requests can be submitted.");
        if (Items.Any(i => i.RequestedQuantity <= 0)) throw new DomainException("Requested quantity must be greater than zero.");
        Status = ReplenishmentStatus.Submitted;
        StockValidationStatus = StockValidationStatus.Pending;
        RejectionReason = null;
        SubmittedAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void SetStockValidationResult(bool passed, string message)
    {
        if (Status != ReplenishmentStatus.Submitted) return;
        StockValidationStatus = passed ? StockValidationStatus.Passed : StockValidationStatus.Failed;
        StockValidationMessage = message;
        Touch();
    }

    public void Approve()
    {
        EnsureStatus(ReplenishmentStatus.Submitted, "Only submitted requests can be approved.");
        if (StockValidationStatus != StockValidationStatus.Passed)
            throw new DomainException("The request cannot be approved until stock validation passes.");
        Status = ReplenishmentStatus.Approved;
        ApprovedAtUtc = DateTime.UtcNow;
        Touch();
    }

    public void Reject(string reason)
    {
        EnsureStatus(ReplenishmentStatus.Submitted, "Only submitted requests can be rejected.");
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("A rejection reason is required.");
        Status = ReplenishmentStatus.Rejected;
        RejectionReason = reason.Trim();
        Touch();
    }

    public void Fulfill(IReadOnlyDictionary<Guid, decimal> fulfilledQuantities)
    {
        EnsureStatus(ReplenishmentStatus.Approved, "Only approved requests can be fulfilled.");
        foreach (var item in Items)
        {
            if (!fulfilledQuantities.TryGetValue(item.Id, out var quantity))
                throw new DomainException($"Fulfilled quantity is required for item {item.ArticleNumber}.");
            if (quantity < 0 || quantity > item.RequestedQuantity)
                throw new DomainException($"Fulfilled quantity for {item.ArticleNumber} must be between 0 and {item.RequestedQuantity}.");
            item.SetFulfilledQuantity(quantity);
        }
        Status = ReplenishmentStatus.Fulfilled;
        FulfilledAtUtc = DateTime.UtcNow;
        Touch();
    }

    private void EnsureStatus(ReplenishmentStatus expected, string message)
    {
        if (Status != expected) throw new DomainException(message);
    }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}

public sealed class ReplenishmentItem
{
    private ReplenishmentItem() { }
    public Guid Id { get; private set; }
    public Guid ReplenishmentRequestId { get; private set; }
    public string ArticleNumber { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal RequestedQuantity { get; private set; }
    public decimal FulfilledQuantity { get; private set; }

    public static ReplenishmentItem Create(string articleNumber, string description, decimal quantity) => new()
    {
        Id = Guid.NewGuid(), ArticleNumber = articleNumber.Trim(), Description = description.Trim(), RequestedQuantity = quantity
    };

    public void SetFulfilledQuantity(decimal quantity) => FulfilledQuantity = quantity;
}

public sealed class DomainException(string message) : Exception(message);
