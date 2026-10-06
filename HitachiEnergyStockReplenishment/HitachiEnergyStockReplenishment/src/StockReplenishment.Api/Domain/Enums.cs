namespace StockReplenishment.Api.Domain;

public enum ReplenishmentStatus { Draft, Submitted, Approved, Fulfilled, Rejected }
public enum RequestPriority { Low, Normal, Urgent }
public enum StockValidationStatus { NotStarted, Pending, Passed, Failed }
