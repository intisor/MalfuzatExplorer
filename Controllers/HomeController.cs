using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using MalfuzatExplorer.Models;
using MalfuzatExplorer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MalfuzatExplorer.Controllers
{
    public class HomeController : Controller
    {
        // ── static: path list is resolved once at startup, not per-request ──
        private static readonly string[] _pdfFiles = new[]
        {
            "Malfuzat-1.pdf", "Malfuzat-2.pdf", "Malfuzat-3.pdf",
            "Malfuzat-4.pdf", "Malfuzat-7.pdf", "Malfuzat-8.pdf",
            "Malfuzat-9.pdf", "Malfuzat-10.pdf"
        };

        private static readonly Regex _arabicRegex =
            new(@"[\u0600-\u06FF\u0750-\u077F\uFB50-\uFDFF\uFE70-\uFEFF]+",
                RegexOptions.Compiled);

        private readonly IMemoryCache _cache;
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly VectorIndexService _vectorIndex;
        private readonly GeminiEmbeddingService _gemini;

        public HomeController(
            IMemoryCache cache,
            ILogger<HomeController> logger,
            IWebHostEnvironment env,
            VectorIndexService vectorIndex,
            GeminiEmbeddingService gemini)
        {
            _cache = cache;
            _logger = logger;
            _env = env;
            _vectorIndex = vectorIndex;
            _gemini = gemini;
        }

        // Returns the full path for a given volume filename.
        private string PdfPath(string fileName) =>
            Path.Combine(_env.WebRootPath, "Malfuzat", fileName);

        public IActionResult Index()
        {
            // Tell the view whether the semantic index is ready
            // so it can show a "Building index…" banner if not
            var model = new MalfuzatModel { IndexReady = _vectorIndex.IsReady };
            return View(model);
        }

        // ── SEMANTIC SEARCH ─────────────────────────────────────────────────
        // LEARNING: This action:
        //   1. Embeds the user's query with taskType RETRIEVAL_QUERY
        //   2. Runs cosine similarity against all stored chunk embeddings
        //   3. Returns the top 10 most semantically similar passages
        [HttpPost]
        public async Task<IActionResult> SemanticSearch(MalfuzatModel model)
        {
            model.SemanticMode = true;
            model.IndexReady = _vectorIndex.IsReady;

            if (string.IsNullOrWhiteSpace(model.Query))
            {
                ModelState.AddModelError("", "Please enter a search query.");
                return View("Index", model);
            }

            if (!_gemini.IsConfigured)
            {
                model.Results = ["Semantic search is unavailable: Gemini API key not configured."];
                return View("Index", model);
            }

            if (!_vectorIndex.IsReady)
            {
                model.Results = ["The semantic index is still building. Please try again in a moment."];
                return View("Index", model);
            }

            try
            {
                // Step 1: Convert the user's query into a vector
                // We use RETRIEVAL_QUERY (docs: optimizes for querying stored documents)
                float[] queryVector = await _gemini.EmbedAsync(model.Query, "RETRIEVAL_QUERY");

                // Step 2: Compare against all stored chunk vectors; take top 10
                var topChunks = _vectorIndex.Search(queryVector, topN: 10);

                // Step 3: Build the result objects the view will display
                model.SemanticResults = topChunks
                    .Select(c => new SemanticResult
                    {
                        Volume = c.Volume,
                        Page = c.Page,
                        Snippet = c.Text
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Semantic search failed for query: {Query}", model.Query);
                model.Results = [$"Semantic search error: {ex.Message}"];
            }

            return View("Index", model);
        }

        [HttpPost]
        public async Task<IActionResult> Search(MalfuzatModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Query))
            {
                ModelState.AddModelError("", "Please enter a valid search query.");
                return View("Index", model);
            }

            string cacheKey = $"search::{model.Query.Trim().ToLowerInvariant()}";

            // ── Cache hit: skip all PDF I/O ──────────────────────────────────
            if (!_cache.TryGetValue(cacheKey, out List<string>? rawResults))
            {
                rawResults = await SearchPdfForQueryAsync(model.Query);

                _cache.Set(cacheKey, rawResults, new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(30),
                    Size = 1
                });
            }

            if (rawResults!.Count == 0)
            {
                model.Results = [$"No results found for '{model.Query}'."];
                return View("Index", model);
            }

            // ── Process all results in parallel (RTL wrap + highlight) ───────
            var processed = await Task.WhenAll(
                rawResults.Select(r => SpecialLanguageAsync(r, model.Query))
            );
            model.Results = [.. processed];

            return View("Index", model);
        }

        public IActionResult Privacy() => View();

        // ── Fast Semantic Search using Pre-computed Gemini Vectors ─────────────────────────────
        public async Task<List<string>> SearchPdfForQueryAsync(string query)
        {
            try
            {
                if (!_vectorIndex.IsReady)
                {
                    return ["The semantic index is still building. Please try again in a moment."];
                }

                // 1. Convert user text query to vector
                float[] queryVector = await _gemini.EmbedAsync(query, "RETRIEVAL_QUERY");
                
                // 2. Perform Cosine Similarity across the cached PDFs
                var topChunks = _vectorIndex.Search(queryVector, topN: 10);

                // 3. Map back to the UI list-of-strings format
                return topChunks.Select(c => $"Found in {c.Volume} on leaf {c.Page}: {c.Text}").ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Semantic search failed for query: {Query}", query);
                return [];
            }
        }

        private static string HighlightQuery(string text, string query) =>
            Regex.Replace(text, Regex.Escape(query),
                $"<mark>{query}</mark>", RegexOptions.IgnoreCase);

        // ── Highlight ONCE, then wrap Arabic runs in RTL span ───────────────
        public Task<string> SpecialLanguageAsync(string result, string query)
        {
            string highlighted = HighlightQuery(result, query);
            string wrapped = _arabicRegex.Replace(highlighted,
                m => $"<span class=\"special\" dir=\"rtl\">{m.Value}</span>");
            return Task.FromResult(wrapped);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
