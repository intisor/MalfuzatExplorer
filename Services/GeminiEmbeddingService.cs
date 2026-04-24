using System.Text;
using System.Text.Json;

namespace MalfuzatExplorer.Services
{
    public class GeminiEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiEmbeddingService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["Gemini:ApiKey"] ?? "";
        }

        // 1. Single Embedding (for Search Queries)
        public async Task<float[]> EmbedAsync(string text, string taskType = "RETRIEVAL_QUERY")
        {
            if (string.IsNullOrWhiteSpace(_apiKey)) throw new InvalidOperationException("API Key missing.");

            // Using the new 2026 standard model
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:embedContent?key={_apiKey}";

            var payload = new
            {
                model = "models/gemini-embedding-001",
                content = new { parts = new[] { new { text = text } } },
                taskType = taskType,
                // Truncating to 768 to keep our vector math efficient and backwards compatible
                outputDimensionality = 768 
            };

            int retries = 0;
            while (retries < 3)
            {
                var response = await _httpClient.PostAsync(url, new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
                
                if (response.IsSuccessStatusCode)
                {
                    return await ParseSingleResponse(response);
                }

                if ((int)response.StatusCode == 429)
                {
                    retries++;
                    if (retries >= 3)
                    {
                        string error = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Gemini 2026 API Error (Rate Limited): {error}");
                    }
                    // Wait before retrying
                    await Task.Delay(2000 * retries);
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Gemini 2026 API Error: {error}");
                }
            }

            throw new Exception("Gemini 2026 API Error: Exceeded retry attempts.");
        }

        // 2. BATCH EMBEDDING (for Fast Indexing)
        public async Task<List<float[]>> BatchEmbedAsync(List<string> texts, string taskType = "RETRIEVAL_DOCUMENT")
        {
            if (string.IsNullOrWhiteSpace(_apiKey)) throw new InvalidOperationException("API Key missing.");

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-embedding-001:batchEmbedContents?key={_apiKey}";

            var requests = texts.Select(t => new
            {
                model = "models/gemini-embedding-001",
                content = new { parts = new[] { new { text = t } } },
                taskType = taskType,
                outputDimensionality = 768
            }).ToList();

            var payload = new { requests = requests };
            var response = await _httpClient.PostAsync(url, new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini Batch 2026 Error: {error}");
            }

            return await ParseBatchResponse(response);
        }

        private async Task<float[]> ParseSingleResponse(HttpResponseMessage response)
        {
            var jsonString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonString);
            return doc.RootElement.GetProperty("embedding").GetProperty("values").EnumerateArray().Select(v => v.GetSingle()).ToArray();
        }

        private async Task<List<float[]>> ParseBatchResponse(HttpResponseMessage response)
        {
            var jsonString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonString);
            var embeddings = new List<float[]>();

            foreach (var item in doc.RootElement.GetProperty("embeddings").EnumerateArray())
            {
                var values = item.GetProperty("values").EnumerateArray().Select(v => v.GetSingle()).ToArray();
                embeddings.Add(values);
            }

            return embeddings;
        }
    }
}
