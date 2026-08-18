using System.Text.Json;
using FlowDiscovery.Api.Models;

namespace FlowDiscovery.Api.Services;

public interface ICognigyClient
{
    /// <summary>
    /// Fetch all flows from Cognigy's management API. If apiKey is supplied, it overrides
    /// the default X-API-Key header configured in Program.cs for this call only.
    /// </summary>
    Task<List<CognigyFlow>> GetFlowsAsync(string? apiKey, CancellationToken ct = default);
}

public class CognigyClient : ICognigyClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CognigyClient> _logger;

    public CognigyClient(HttpClient httpClient, ILogger<CognigyClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<CognigyFlow>> GetFlowsAsync(string? apiKey, CancellationToken ct = default)
    {
        // BaseAddress and the default X-API-Key header are already set on this HttpClient
        // in Program.cs's AddHttpClient<ICognigyClient, CognigyClient> factory. We only need
        // to override the header here if the caller supplied a different key for this request.
        using var request = new HttpRequestMessage(HttpMethod.Get, "v2.0/flows");
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Remove("X-API-Key");
            request.Headers.Add("X-API-Key", apiKey);
        }

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);

        // Cognigy's list endpoints commonly wrap results in an "items" array; fall back to
        // treating the root as the array if "items" isn't present. VERIFY this against a real
        // response from your own tenant with Postman/curl before trusting it -- see the
        // Prototype 4 guide's Part A for the manual test.
        var items = doc.RootElement.TryGetProperty("items", out var itemsProp)
            ? itemsProp
            : doc.RootElement;

        var flows = new List<CognigyFlow>();
        foreach (var item in items.EnumerateArray())
        {
            flows.Add(new CognigyFlow
            {
                Id = TryGetString(item, "_id") ?? TryGetString(item, "id") ?? string.Empty,
                Name = TryGetString(item, "name") ?? string.Empty,
                Description = TryGetString(item, "description") ?? string.Empty,
                ProjectName = TryGetString(item, "projectName") ?? string.Empty,
                // ExtractedIntents/Keywords are populated later by FlowCacheService via
                // the Prompt Manager -- Cognigy's own flow list doesn't return these.
                ExtractedIntents = new List<string>(),
                Keywords = new List<string>()
            });
        }

        _logger.LogInformation("Fetched {Count} flows from Cognigy", flows.Count);
        return flows;
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;
    }
}
