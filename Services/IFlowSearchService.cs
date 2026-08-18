using FlowDiscovery.Api.Models;

namespace FlowDiscovery.Api.Services;

public interface IFlowSearchService
{
    Task<FlowSearchResponse> SearchAsync(FlowSearchRequest request, CancellationToken ct = default);
}
