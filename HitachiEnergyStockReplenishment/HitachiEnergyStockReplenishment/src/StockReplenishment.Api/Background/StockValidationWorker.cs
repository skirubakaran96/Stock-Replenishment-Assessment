using Microsoft.EntityFrameworkCore;
using StockReplenishment.Api.Application;
using StockReplenishment.Api.Infrastructure;

namespace StockReplenishment.Api.Background;

public sealed class StockValidationWorker(
    IStockValidationQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<StockValidationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var requestId in ReadQueue(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var stockService = scope.ServiceProvider.GetRequiredService<IStockAvailabilityService>();
                var request = await db.ReplenishmentRequests.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == requestId, stoppingToken);
                if (request is null) continue;

                var result = await stockService.CheckAsync(request, stoppingToken);
                request.SetStockValidationResult(result.IsAvailable, result.Message);
                await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "Stock validation failed for request {RequestId}", requestId);
                try
                {
                    using var errorScope = scopeFactory.CreateScope();
                    var errorDb = errorScope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var failedRequest = await errorDb.ReplenishmentRequests.SingleOrDefaultAsync(x => x.Id == requestId, stoppingToken);
                    if (failedRequest is not null)
                    {
                        failedRequest.SetStockValidationResult(false, "Stock validation could not be completed. Please reject and create a new request.");
                        await errorDb.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception persistenceException)
                {
                    logger.LogError(persistenceException, "Unable to persist stock validation failure for request {RequestId}", requestId);
                }
            }
        }
    }

    private async IAsyncEnumerable<Guid> ReadQueue([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
    {
        while (!token.IsCancellationRequested)
            yield return await queue.DequeueAsync(token);
    }
}
