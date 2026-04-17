using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MalfuzatExplorer.Services;

/// <summary>
/// Calls the Gemini Embedding REST API to convert text → float[].
/// 
/// ENDPOINT (from docs):
///   POST https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent
///   Header: x-goog-api-key: {key}
///   Body:   { "taskType": "RETRIEVAL_DOCUMENT", "content": { "parts": [{ "text": "..." }] } }
/// 
/// RESPONSE:
///   { "embedding": { "values": [0.12, -0.87, ...] } }   ← 768 floats
/// </summary>
public sealed class GeminiEmbeddingService
{
    // ── Constants from the Google AI docs ──────────────────────────────────
    private const string Model = "gemini-embedding-2-preview";
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";
    private const int MaxRetry = 3;

    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly ILogger<GeminiEmbeddingService> _logger;

    public GeminiEmbeddingService(
        HttpClient http,
        IConfiguration config,
        ILogger<GeminiEmbeddingService> logger)
    {
        _http = http;
        _apiKey = config["Gemini:ApiKey"] ?? string.Empty;
        _logger = logger;
    }

    /// <summary>
    /// Returns true if service is usable (key is configured).
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    /// <summary>
    /// Embed a single piece of text.
    /// 
    /// taskType should be:
    ///   "RETRIEVAL_DOCUMENT" — when indexing PDF chunks at startup
    ///   "RETRIEVAL_QUERY"    — when embedding the user's live search query
    /// </summary>
    public async Task<float[]> EmbedAsync(string text, string taskType = "RETRIEVAL_DOCUMENT")
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Gemini API key is not configured.");

        // Trim to the model's 2 048-token input limit.
        // Latin: ~4 chars/token → 8 192 chars safe.
        // Urdu/Arabic: ~1–2 chars/token → 4 000 chars is the safe upper bound.
        if (text.Length > 4000)
            text = text[..4000];

        // Apply Embeddings 2 explicit prompt formatting instead of sending a taskType parameter
        string formattedText = taskType switch 
        {
            "RETRIEVAL_DOCUMENT" => $"title: Malfuzat Document | text: {text}",
            "RETRIEVAL_QUERY" => $"task: search result | query: {text}",
            _ => text
        };

        var url = $"{BaseUrl}{Model}:embedContent?key={_apiKey}";

        // ── Build the JSON body exactly as the docs specify ─────────────
        var body = new EmbedRequest(new Content([new Part(formattedText)]));

        for (int attempt = 1; attempt <= MaxRetry; attempt++)
        {
            try
            {
                var response = await _http.PostAsJsonAsync(url, body);

                if (!response.IsSuccessStatusCode)
                {
                    string err = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Gemini embed attempt {A} failed: {S} {E}",
                        attempt, response.StatusCode, err);

                    if (attempt == MaxRetry) throw new HttpRequestException(
                        $"Gemini API failed after {MaxRetry} attempts: {err}");

                    // Respect rate-limit: back off before retrying
                    await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
                    continue;
                }

                var result = await response.Content.ReadFromJsonAsync<EmbedResponse>();
                return result?.Embedding?.Values
                    ?? throw new InvalidDataException("Gemini returned empty embedding.");
            }
            catch (HttpRequestException) when (attempt < MaxRetry)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2));
            }
        }

        throw new InvalidOperationException("Embedding failed after all retries.");
    }

    // ── Request / Response shapes (mirroring the REST API body exactly) ──

    private record EmbedRequest(
        [property: JsonPropertyName("content")] Content Content);

    private record Content(
        [property: JsonPropertyName("parts")] Part[] Parts);

    private record Part(
        [property: JsonPropertyName("text")] string Text);

    private record EmbedResponse(
        [property: JsonPropertyName("embedding")] EmbeddingValues? Embedding);

    private record EmbeddingValues(
        [property: JsonPropertyName("values")] float[] Values);
}
