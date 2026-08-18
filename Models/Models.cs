namespace FlowDiscovery.Api.Models;

// ---- Request/response DTOs for the search endpoint ----

public class FlowSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public int TopN { get; set; } = 5;
}

public class FlowSearchResponse
{
    public List<FlowSearchResult> Results { get; set; } = new();
    public string Query { get; set; } = string.Empty;
    public List<string> ExtractedIntents { get; set; } = new();
    public List<string> ExtractedKeywords { get; set; } = new();
    public int TotalFlowsSearched { get; set; }
    public DateTime SearchedAt { get; set; }
}

public class FlowSearchResult
{
    public string FlowId { get; set; } = string.Empty;
    public string FlowName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public double Score { get; set; }
    public List<string> MatchedIntents { get; set; } = new();
    public List<string> MatchedKeywords { get; set; } = new();
    public ScoreBreakdown Breakdown { get; set; } = new();
}

public class ScoreBreakdown
{
    public int ExactIntentMatches { get; set; }
    public int PartialIntentMatches { get; set; }
    public int KeywordMatches { get; set; }
    public double RawScore { get; set; }
    public double NormalizedScore { get; set; }
}

// ---- Cognigy flow metadata ----

public class CognigyFlow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public List<string> ExtractedIntents { get; set; } = new();
    public List<string> Keywords { get; set; } = new();
}

// ---- Prompt Manager intent extraction DTOs ----

public class IntentExtractionRequest
{
    public string Text { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
}

public class IntentExtractionResponse
{
    public List<string> Intents { get; set; } = new();
    public List<string> Keywords { get; set; } = new();
}
