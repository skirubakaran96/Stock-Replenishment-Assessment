using NUnit.Framework;
using StockReplenishment.Api.Domain;

namespace StockReplenishment.Tests;

[TestFixture]
public sealed class ReplenishmentRequestTests
{
    [Test]
    public void Submit_FromDraft_StartsPendingStockValidation()
    {
        var request = CreateRequest();

        request.Submit();

        Assert.That(request.Status, Is.EqualTo(ReplenishmentStatus.Submitted));
        Assert.That(request.StockValidationStatus, Is.EqualTo(StockValidationStatus.Pending));
    }

    [Test]
    public void Approve_BeforeValidationPasses_Throws()
    {
        var request = CreateRequest();
        request.Submit();

        var ex = Assert.Throws<DomainException>(() => request.Approve());

        Assert.That(ex!.Message, Does.Contain("stock validation"));
    }

    [Test]
    public void Reject_RequiresReason()
    {
        var request = CreateRequest();
        request.Submit();

        Assert.Throws<DomainException>(() => request.Reject(" "));
    }

    [Test]
    public void Fulfill_CannotExceedRequestedQuantity()
    {
        var request = CreateRequest();
        request.Submit();
        request.SetStockValidationResult(true, "Available");
        request.Approve();
        var itemId = request.Items.Single().Id;

        var ex = Assert.Throws<DomainException>(() => request.Fulfill(new Dictionary<Guid, decimal> { [itemId] = 11 }));

        Assert.That(ex!.Message, Does.Contain("between 0 and 10"));
    }

    private static ReplenishmentRequest CreateRequest() => ReplenishmentRequest.Create(
        "Line A", RequestPriority.Normal,
        [ReplenishmentItem.Create("MAT-1", "Test Material", 10)]);
}
