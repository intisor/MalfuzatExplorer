using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using MalfuzatExplorer.Models;
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

        public HomeController(
            IMemoryCache cache,
            ILogger<HomeController> logger,
            IWebHostEnvironment env)
        {
            _cache = cache;
            _logger = logger;
            _env = env;
        }

        // Returns the full path for a given volume filename.
        private string PdfPath(string fileName) =>
            Path.Combine(_env.WebRootPath, "Malfuzat", fileName);

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

        public async Task<List<string>> SearchPdfForQueryAsync(string query)
        {
            var results = new List<string>();
            await Task.Run(() => {
                foreach (var vol in _pdfFiles)
                {
                    var path = PdfPath(vol);
                    if (!System.IO.File.Exists(path)) continue;

                    try 
                    {
                        using var pdfDoc = new PdfDocument(new PdfReader(path));
                        int numPages = pdfDoc.GetNumberOfPages();

                        for (int i = 1; i <= numPages; i++)
                        {
                            var page = pdfDoc.GetPage(i);
                            var text = PdfTextExtractor.GetTextFromPage(page);

                            if (!string.IsNullOrEmpty(text) && text.Contains(query, StringComparison.OrdinalIgnoreCase))
                            {
                                var matchIndex = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
                                int start = Math.Max(0, matchIndex - 50);
                                int length = Math.Min(text.Length - start, query.Length + 100);
                                string snippet = text.Substring(start, length).Replace("\n", " ").Replace("\r", "");
                                
                                results.Add($"Volume: {vol}, Page: {i} - ...{snippet}...");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading {Volume}", vol);
                    }
                }
            });
            return results;
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
