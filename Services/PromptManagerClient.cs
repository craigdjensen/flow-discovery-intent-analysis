using System.Net.Http.Json;
using FlowDiscovery.Api.Models;

namespace FlowDiscovery.Api.Services;

public interface IPromptManagerClient
{
    Task<IntentExtractionResponse> ExtractIntentsAsync(IntentExtractionRequest request, CancellationToken ct = default);
}

public class PromptManagerClient : IPromptManagerClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<PromptManagerClient> _logger;

    public PromptManagerClient(HttpClient httpClient, IConfiguration config, ILogger<PromptManagerClient> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<IntentExtractionResponse> ExtractIntentsAsync(IntentExtractionRequest request, CancellationToken ct = default)
    {
        // NOTE: this project's HttpClient (see Program.cs) doesn't set a BaseAddress for
        // IPromptManagerClient, so the full URL is built here from config on every call.
        //
        // IMPORTANT / UNVERIFIED: the real Enlighten Prompt Manager service is a *templated*
        // prompt-execution API (execute a named+versioned template with parameters), not a
        // generic "send text, get intents back" endpoint. The shape below is a stand-in that
        // matches this project's existing IntentExtractionRequest/Response DTOs so the rest of
        // the app compiles and runs against a mock/local stub. Once you have real Prompt
        // Manager access (see the Prototype 4 guide's Part C follow-up), you'll likely need to
        // change this method's body -- not its signature -- to call the actual execute endpoint
        // with your approved template name/version and parameter map.
        var baseUrl = _config["PromptManager:BaseUrl"];
        var path = _config["PromptManager:ExtractIntentsPath"] ?? "/api/intents/extract";
        var apiKeyHeader = _config["PromptManager:ApiKeyHeaderName"] ?? "prompt-manager-api-key";
        var apiKey = _config["PromptManager:ApiKey"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogWarning("PromptManager:BaseUrl is not configured -- returning empty intents (degraded mode)");
            return new IntentExtractionResponse();
        }

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}{path}")
            {
                Content = JsonContent.Create(request)
            };
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                httpRequest.Headers.Add(apiKeyHeader, apiKey);
            }

            var response = await _httpClient.SendAsync(httpRequest, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<IntentExtractionResponse>(cancellationToken: ct);
            return result ?? new IntentExtractionResponse();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Prompt Manager unreachable -- falling back to empty intents for text: {Text}", request.Text);
            return new IntentExtractionResponse(); // degraded mode: search still works on keyword matching alone
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Prompt Manager request timed out for text: {Text}", request.Text);
            return new IntentExtractionResponse();
        }
    }
}
