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
        private readonly MalfuzatSearchService _searchService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _env;

        private static readonly Regex _arabicRegex =
            new(@"[\u0600-\u06FF\u0750-\u077F\uFB50-\uFDFF\uFE70-\uFEFF]+",
                RegexOptions.Compiled);

        public HomeController(
            MalfuzatSearchService searchService,
            IMemoryCache cache,
            ILogger<HomeController> logger,
            IWebHostEnvironment env)
        {
            _searchService = searchService;
            _cache = cache;
            _logger = logger;
            _env = env;
        }

        public IActionResult Index()
        {
            return View(new MalfuzatModel());
        }

        [HttpPost]
        public async Task<IActionResult> Search(MalfuzatModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Query))
            {
                ModelState.AddModelError("", "Please enter a valid search query.");
                return View("Index", model);
            }

            if (!_searchService.IsLoaded)
            {
                model.Results = new List<string> { "Search engine is still initializing or no data found. Please wait a moment and try again." };
                return View("Index", model);
            }

            string cacheKey = $"semantic_search::{model.Query.Trim().ToLowerInvariant()}";

            if (!_cache.TryGetValue(cacheKey, out List<string>? processedResults))
            {
                var searchResults = await _searchService.SearchAsync(model.Query, topK: 5);

                processedResults = new List<string>();
                foreach (var res in searchResults)
                {
                    string snippet = res.Chunk.Text;
                    string volume = res.Chunk.Volume;
                    int page = res.Chunk.Page;
                    float score = res.Score;

                    string formatted = $"<strong>Volume: {volume}, Page: {page}</strong> (Relevance: {score:P0})<br/>{snippet}";
                    processedResults.Add(await SpecialLanguageAsync(formatted, model.Query));
                }

                _cache.Set(cacheKey, processedResults, new MemoryCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(30),
                    Size = 1
                });
            }

            model.Results = processedResults ?? new List<string>();
            return View("Index", model);
        }

        public IActionResult Privacy() => View();

        private static string HighlightQuery(string text, string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return text;
            return Regex.Replace(text, Regex.Escape(query),
                $"<mark>{query}</mark>", RegexOptions.IgnoreCase);
        }

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
