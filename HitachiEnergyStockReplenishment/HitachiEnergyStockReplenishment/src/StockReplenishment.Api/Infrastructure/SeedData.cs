using Microsoft.EntityFrameworkCore;
using StockReplenishment.Api.Domain;

namespace StockReplenishment.Api.Infrastructure;

public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        if (await db.ReplenishmentRequests.AnyAsync()) return;

        var requests = new[]
        {
            ReplenishmentRequest.Create("Line A - Assembly", RequestPriority.Urgent, new[]
            {
                ReplenishmentItem.Create("MAT-1001", "Bearing 6204", 20),
                ReplenishmentItem.Create("MAT-1002", "Hex Bolt M8", 100)
            }),
            ReplenishmentRequest.Create("Line B - Welding", RequestPriority.Normal, new[]
            {
                ReplenishmentItem.Create("MAT-2001", "Welding Wire 1.2mm", 15)
            }),
            ReplenishmentRequest.Create("Line C - Testing", RequestPriority.Low, new[]
            {
                ReplenishmentItem.Create("MAT-3001", "Test Connector", 8)
            })
        };

        requests[0].Submit();
        requests[0].SetStockValidationResult(true, "Seeded validation: stock available.");
        requests[0].Approve();

        requests[1].Submit();
        requests[1].SetStockValidationResult(false, "Seeded validation: insufficient stock for requested quantity.");

        db.ReplenishmentRequests.AddRange(requests);
        await db.SaveChangesAsync();
    }
}
