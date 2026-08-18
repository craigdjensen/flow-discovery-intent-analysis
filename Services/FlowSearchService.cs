using FlowDiscovery.Api.Models;

namespace FlowDiscovery.Api.Services;

public class FlowSearchService : IFlowSearchService
{
    private const int ExactMatchScore = 50;
    private const int PartialMatchScore = 30;
    private const int KeywordMatchScore = 20;

    private readonly IFlowCacheService _cache;
    private readonly IPromptManagerClient _promptManager;
    private readonly ILogger<FlowSearchService> _logger;

    public FlowSearchService(
        IFlowCacheService cache,
        IPromptManagerClient promptManager,
        ILogger<FlowSearchService> logger)
    {
        _cache = cache;
        _promptManager = promptManager;
        _logger = logger;
    }

    public async Task<FlowSearchResponse> SearchAsync(FlowSearchRequest request, CancellationToken ct = default)
    {
        // 1. Extract intents from the query
        var queryIntents = await _promptManager.ExtractIntentsAsync(
            new IntentExtractionRequest { Text = request.Query, Context = "user_query" }, ct);

        // 2. Get cached flows
        var flows = await _cache.GetFlowsAsync(request.ApiKey, ct);

        // 3. Score each flow
        var results = flows
            .Select(flow => ScoreFlow(flow, queryIntents))
            .Where(r => r.Score > 0)
            .OrderByDescending(r => r.Score)
            .Take(request.TopN)
            .ToList();

        _logger.LogInformation("Query '{Query}' matched {Count}/{Total} flows",
            request.Query, results.Count, flows.Count);

        return new FlowSearchResponse
        {
            Results = results,
            Query = request.Query,
            ExtractedIntents = queryIntents.Intents,
            ExtractedKeywords = queryIntents.Keywords,
            TotalFlowsSearched = flows.Count,
            SearchedAt = DateTime.UtcNow
        };
    }

    private FlowSearchResult ScoreFlow(CognigyFlow flow, IntentExtractionResponse queryIntents)
    {
        var matchedIntents = new List<string>();
        var matchedKeywords = new List<string>();
        var breakdown = new ScoreBreakdown();
        double totalScore = 0;
        int comparisons = 0;

        foreach (var queryIntent in queryIntents.Intents)
        {
            foreach (var flowIntent in flow.ExtractedIntents)
            {
                comparisons++;
                if (string.Equals(queryIntent, flowIntent, StringComparison.OrdinalIgnoreCase))
                {
                    totalScore += ExactMatchScore;
                    breakdown.ExactIntentMatches++;
                    matchedIntents.Add(flowIntent);
                }
                else if (flowIntent.Contains(queryIntent, StringComparison.OrdinalIgnoreCase)
                      || queryIntent.Contains(flowIntent, StringComparison.OrdinalIgnoreCase))
                {
                    totalScore += PartialMatchScore;
                    breakdown.PartialIntentMatches++;
                    matchedIntents.Add(flowIntent);
                }
            }

            // Keyword matching against flow name + description
            var flowText = $"{flow.Name} {flow.Description}".ToLowerInvariant();
            if (flowText.Contains(queryIntent.ToLowerInvariant()))
            {
                totalScore += KeywordMatchScore;
                breakdown.KeywordMatches++;
                matchedKeywords.Add(queryIntent);
            }
        }

        // Also check query keywords against flow keywords
        foreach (var kw in queryIntents.Keywords)
        {
            if (flow.Keywords.Any(fk => string.Equals(fk, kw, StringComparison.OrdinalIgnoreCase)))
            {
                totalScore += KeywordMatchScore;
                breakdown.KeywordMatches++;
                matchedKeywords.Add(kw);
            }
        }

        var avgScore = comparisons > 0 ? totalScore / comparisons : 0;
        breakdown.RawScore = totalScore;
        breakdown.NormalizedScore = Math.Round(avgScore, 2);

        return new FlowSearchResult
        {
            FlowId = flow.Id,
            FlowName = flow.Name,
            Description = flow.Description,
            ProjectName = flow.ProjectName,
            Score = Math.Round(totalScore, 2),
            MatchedIntents = matchedIntents.Distinct().ToList(),
            MatchedKeywords = matchedKeywords.Distinct().ToList(),
            Breakdown = breakdown
        };
    }
}
