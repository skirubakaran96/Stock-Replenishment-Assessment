using Microsoft.EntityFrameworkCore;
using NSubstitute;
using NUnit.Framework;
using StockReplenishment.Api.Application;
using StockReplenishment.Api.Background;
using StockReplenishment.Api.Domain;
using StockReplenishment.Api.Infrastructure;

namespace StockReplenishment.Tests;

[TestFixture]
public sealed class ReplenishmentRequestServiceTests
{
    private AppDbContext _db = null!;
    private IStockValidationQueue _queue = null!;
    private ReplenishmentRequestService _service = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _db = new AppDbContext(options);
        _queue = Substitute.For<IStockValidationQueue>();
        _service = new ReplenishmentRequestService(_db, _queue);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task Submit_EnqueuesValidation_WithoutWaitingForExternalService()
    {
        var created = await _service.CreateAsync(
            new CreateReplenishmentRequest("Line A", RequestPriority.Urgent,
                [new CreateReplenishmentItem("MAT-1", "Bearing", 5)]), CancellationToken.None);

        await _service.SubmitAsync(created.Id, CancellationToken.None);

        var detail = await _service.GetByIdAsync(created.Id, CancellationToken.None);
        Assert.That(detail!.Status, Is.EqualTo(ReplenishmentStatus.Submitted));
        Assert.That(detail.StockValidationStatus, Is.EqualTo(StockValidationStatus.Pending));
        await _queue.Received(1).EnqueueAsync(created.Id, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetAsync_AppliesFiltersAndPagination()
    {
        await _service.CreateAsync(new CreateReplenishmentRequest("Line A", RequestPriority.Urgent,
            [new CreateReplenishmentItem("A", "A", 1)]), CancellationToken.None);
        await _service.CreateAsync(new CreateReplenishmentRequest("Line B", RequestPriority.Low,
            [new CreateReplenishmentItem("B", "B", 1)]), CancellationToken.None);

        var result = await _service.GetAsync(null, RequestPriority.Urgent, "Line A", 1, 10, CancellationToken.None);

        Assert.That(result.TotalCount, Is.EqualTo(1));
        Assert.That(result.Items.Single().Location, Is.EqualTo("Line A"));
    }
}
