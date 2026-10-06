using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using StockReplenishment.Api.Application;
using StockReplenishment.Api.Background;
using StockReplenishment.Api.Domain;
using StockReplenishment.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("StockReplenishmentDb"));

builder.Services.AddScoped<IReplenishmentRequestService, ReplenishmentRequestService>();
builder.Services.AddScoped<IStockAvailabilityService, SimulatedStockAvailabilityService>();
builder.Services.AddSingleton<IStockValidationQueue, StockValidationQueue>();
builder.Services.AddHostedService<StockValidationWorker>();

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5050";
builder.Services.AddHttpClient("Api", client => client.BaseAddress = new Uri(apiBaseUrl));
builder.Services.AddScoped<StockReplenishment.Api.Web.ApiClient>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await SeedData.InitializeAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.MapControllers();
app.MapRazorComponents<StockReplenishment.Api.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program { }
