using System.Net.Http.Json;
using StockReplenishment.Api.Application;
using StockReplenishment.Api.Domain;

namespace StockReplenishment.Api.Web;

public sealed class ApiClient(IHttpClientFactory httpClientFactory)
{
    private HttpClient Client => httpClientFactory.CreateClient("Api");

    public async Task<PagedResult<ReplenishmentListItem>> GetRequestsAsync(ReplenishmentStatus? status, RequestPriority? priority, string? location, int page, int pageSize, CancellationToken token = default)
    {
        var query = $"api/replenishment-requests?pageNumber={page}&pageSize={pageSize}";
        if (status.HasValue) query += $"&status={status}";
        if (priority.HasValue) query += $"&priority={priority}";
        if (!string.IsNullOrWhiteSpace(location)) query += $"&location={Uri.EscapeDataString(location)}";
        return await Client.GetFromJsonAsync<PagedResult<ReplenishmentListItem>>(query, token) ?? new([], page, pageSize, 0, 0);
    }

    public Task<ReplenishmentDetail?> GetAsync(Guid id, CancellationToken token = default) => Client.GetFromJsonAsync<ReplenishmentDetail>($"api/replenishment-requests/{id}", token);

    public async Task<(bool Success, string? Error, ReplenishmentDetail? Data)> CreateAsync(CreateReplenishmentRequest request, CancellationToken token = default)
    {
        var response = await Client.PostAsJsonAsync("api/replenishment-requests", request, token);
        if (!response.IsSuccessStatusCode) return (false, await ReadError(response), null);
        return (true, null, await response.Content.ReadFromJsonAsync<ReplenishmentDetail>(cancellationToken: token));
    }

    public async Task<(bool Success, string? Error)> PostAsync(string path, object? body = null, CancellationToken token = default)
    {
        var response = body is null ? await Client.PostAsync(path, null, token) : await Client.PostAsJsonAsync(path, body, token);
        return response.IsSuccessStatusCode ? (true, null) : (false, await ReadError(response));
    }

    public async Task<StockValidationResult?> GetValidationAsync(Guid id, CancellationToken token = default)
        => await Client.GetFromJsonAsync<StockValidationResult>($"api/replenishment-requests/{id}/stock-validation", token);

    private static async Task<string> ReadError(HttpResponseMessage response)
    {
        try
        {
            var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
            return payload?.Error ?? $"Request failed ({(int)response.StatusCode} {response.StatusCode}).";
        }
        catch { return $"Request failed ({(int)response.StatusCode} {response.StatusCode})."; }
    }

    private sealed record ErrorResponse(string Error);
}
