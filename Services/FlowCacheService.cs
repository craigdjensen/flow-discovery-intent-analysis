using FlowDiscovery.Api.Models;
using Microsoft.Extensions.Caching.Memory;

namespace FlowDiscovery.Api.Services;

public interface IFlowCacheService
{
    Task<List<CognigyFlow>> GetFlowsAsync(string? apiKey, CancellationToken ct = default);
    Task InvalidateCacheAsync();
    DateTime? LastRefreshed { get; }
}

public class FlowCacheService : IFlowCacheService
{
    private const string CacheKey = "cognigy-flows-with-intents";

    private readonly IMemoryCache _cache;
    private readonly ICognigyClient _cognigyClient;
    private readonly IPromptManagerClient _promptManagerClient;
    private readonly ILogger<FlowCacheService> _logger;
    private readonly TimeSpan _cacheTtl;

    public DateTime? LastRefreshed { get; private set; }

    public FlowCacheService(
        ICognigyClient cognigyClient,
        IPromptManagerClient promptManagerClient,
        IMemoryCache cache,
        IConfiguration config,
        ILogger<FlowCacheService> logger)
    {
        _cognigyClient = cognigyClient;
        _promptManagerClient = promptManagerClient;
        _cache = cache;
        _logger = logger;

        // Defaults to 5 minutes to match the doc's "refreshes every 5 minutes" behavior;
        // override via FlowCache:RefreshMinutes in appsettings.json if needed.
        var minutes = config.GetValue<int?>("FlowCache:RefreshMinutes") ?? 5;
        _cacheTtl = TimeSpan.FromMinutes(minutes);
    }

    public async Task<List<CognigyFlow>> GetFlowsAsync(string? apiKey, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey, out List<CognigyFlow>? cached) && cached is not null)
        {
            return cached;
        }

        _logger.LogInformation("Flow cache miss -- fetching fresh data from Cognigy and Prompt Manager");
        var flows = await _cognigyClient.GetFlowsAsync(apiKey, ct);

        foreach (var flow in flows)
        {
            var extraction = await _promptManagerClient.ExtractIntentsAsync(
                new IntentExtractionRequest { Text = flow.Description, Context = "flow_description" }, ct);
            flow.ExtractedIntents = extraction.Intents;
            flow.Keywords = extraction.Keywords;
        }

        _cache.Set(CacheKey, flows, _cacheTtl);
        LastRefreshed = DateTime.UtcNow;

        return flows;
    }

    public Task InvalidateCacheAsync()
    {
        _cache.Remove(CacheKey);
        _logger.LogInformation("Flow cache invalidated manually");
        return Task.CompletedTask;
    }
}
