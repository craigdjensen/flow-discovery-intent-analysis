using FlowDiscovery.Api.Models;
using FlowDiscovery.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FlowDiscovery.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlowDiscoveryController : ControllerBase
{
    private readonly IFlowSearchService _searchService;
    private readonly IFlowCacheService _cacheService;
    private readonly ILogger<FlowDiscoveryController> _logger;

    public FlowDiscoveryController(
        IFlowSearchService searchService,
        IFlowCacheService cacheService,
        ILogger<FlowDiscoveryController> logger)
    {
        _searchService = searchService;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Search flows by a natural-language customer query.
    /// </summary>
    [HttpPost("search")]
    [ProducesResponseType(typeof(FlowSearchResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Search([FromBody] FlowSearchRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return BadRequest(new { error = "Query is required." });

        var result = await _searchService.SearchAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// List all flows currently in the cache.
    /// </summary>
    [HttpGet("flows")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetFlows([FromQuery] string? apiKey, CancellationToken ct)
    {
        var flows = await _cacheService.GetFlowsAsync(apiKey, ct);
        return Ok(new
        {
            count = flows.Count,
            lastRefreshed = _cacheService.LastRefreshed,
            flows
        });
    }

    /// <summary>
    /// Force a cache refresh — next request will re-fetch from Cognigy.
    /// </summary>
    [HttpDelete("cache")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> InvalidateCache()
    {
        await _cacheService.InvalidateCacheAsync();
        return NoContent();
    }
}
