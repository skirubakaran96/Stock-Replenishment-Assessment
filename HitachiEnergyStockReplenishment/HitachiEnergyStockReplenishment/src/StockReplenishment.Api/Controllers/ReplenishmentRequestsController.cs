using Microsoft.AspNetCore.Mvc;
using StockReplenishment.Api.Application;
using StockReplenishment.Api.Domain;

namespace StockReplenishment.Api.Controllers;

[ApiController]
[Route("api/replenishment-requests")]
public sealed class ReplenishmentRequestsController(IReplenishmentRequestService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ReplenishmentListItem>>> Get(
        [FromQuery] ReplenishmentStatus? status,
        [FromQuery] RequestPriority? priority,
        [FromQuery] string? location,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
        => Ok(await service.GetAsync(status, priority, location, pageNumber, pageSize, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReplenishmentDetail>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ReplenishmentDetail>> Create(CreateReplenishmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ValidationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await service.SubmitAsync(id, cancellationToken);
            return AcceptedAtAction(nameof(GetStockValidation), new { id });
        }
        catch (NotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (DomainException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpGet("{id:guid}/stock-validation")]
    public async Task<ActionResult<StockValidationResult>> GetStockValidation(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetStockValidationAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
        => await Execute(id, () => service.ApproveAsync(id, cancellationToken));

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, RejectRequest request, CancellationToken cancellationToken)
        => await Execute(id, () => service.RejectAsync(id, request.Reason, cancellationToken));

    [HttpPost("{id:guid}/fulfill")]
    public async Task<IActionResult> Fulfill(Guid id, FulfillRequest request, CancellationToken cancellationToken)
    {
        if (request.Items is null) return BadRequest(new { error = "Fulfillment items are required." });
        return await Execute(id, () => service.FulfillAsync(id, request.Items.ToDictionary(x => x.ItemId, x => x.FulfilledQuantity), cancellationToken));
    }

    private static async Task<IActionResult> Execute(Guid id, Func<Task> action)
    {
        try { await action(); return new NoContentResult(); }
        catch (NotFoundException ex) { return new NotFoundObjectResult(new { error = ex.Message }); }
        catch (DomainException ex) { return new ConflictObjectResult(new { error = ex.Message }); }
        catch (ArgumentException ex) { return new BadRequestObjectResult(new { error = ex.Message }); }
    }
}
