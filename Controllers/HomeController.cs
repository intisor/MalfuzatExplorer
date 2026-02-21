using MalfuzatExplorer.DTOs;
using MalfuzatExplorer.Models;
using MalfuzatExplorer.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace MalfuzatExplorer.Controllers
{
    public class HomeController : Controller
    {
        private readonly IPdfCacheService _pdfCache;
        private readonly ILogger<HomeController> _logger;

        private static readonly string[] PdfFiles =
        {
            "Malfuzat-1.pdf",
            "Malfuzat-2.pdf",
            "Malfuzat-3.pdf",
            "Malfuzat-4.pdf",
            "Malfuzat-7.pdf",
            "Malfuzat-8.pdf",
            "Malfuzat-9.pdf",
            "Malfuzat-10.pdf"
        };

        public HomeController(IPdfCacheService pdfCache, ILogger<HomeController> logger)
        {
            _pdfCache = pdfCache;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var model = new MalfuzatModel();
            return View(model);
        }

        [HttpGet]
        public IActionResult CacheStatus()
        {
            return Json(new { isLoaded = _pdfCache.IsLoaded });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(MalfuzatModel model)
        {
            if (string.IsNullOrEmpty(model.Query))
            {
                ModelState.AddModelError("", "Please enter a valid search query.");
                return View("Index", model);
            }

            var results = await SearchPdfForQueryAsync(model.Query);
            model.TotalResults = results.Count;
            model.Results = results;
            return View("Index", model);
        }
        
        public IActionResult Privacy()
        {
            return View();
        }

        private async Task<List<SearchResultDto>> SearchPdfForQueryAsync(string query)
        {
            if (!_pdfCache.IsLoaded)
                return new List<SearchResultDto>();

            try
            {
                var bag = new ConcurrentBag<SearchResultDto>();
                var tasks = PdfFiles.Select(pdfFileName => Task.Run(() =>
                {
                    var pages = _pdfCache.GetPages(pdfFileName);
                    if (pages is null)
                    {
                        _logger.LogWarning("PDF not found in cache: {FileName}", pdfFileName);
                        return;
                    }

                    for (int i = 0; i < pages.Length; i++)
                    {
                        if (pages[i].Contains(query, StringComparison.OrdinalIgnoreCase))
                        {
                            bag.Add(new SearchResultDto
                            {
                                Volume = Path.GetFileNameWithoutExtension(pdfFileName),
                                PageNumber = i + 1,
                                Snippet = BuildSnippet(pages[i], query)
                            });
                        }
                    }
                }));

                await Task.WhenAll(tasks).ConfigureAwait(false);
                return bag.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during search for query: {Query}", query);
                return new List<SearchResultDto>();
            }
        }

        /// <summary>
        /// Extracts a 200-word context window around the query term, HTML-encodes the raw
        /// page text first (XSS fix), then re-injects safe &lt;mark&gt; and RTL span tags.
        /// Synchronous — no I/O, no Task.Run wrapper needed.
        /// </summary>
        private static string BuildSnippet(string pageText, string query)
        {
            query = query.Trim();

            // XSS fix: encode raw page content BEFORE any HTML is injected
            string encoded = HtmlEncoder.Default.Encode(pageText);
            string encodedQuery = HtmlEncoder.Default.Encode(query);

            string[] words = encoded.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            int idx = Array.FindIndex(words, w => w.Contains(encodedQuery, StringComparison.OrdinalIgnoreCase));
            if (idx < 0) return string.Empty;

            int start = Math.Max(0, idx - 100);
            int end = Math.Min(words.Length, idx + 101);
            string context = string.Join(" ", words.Skip(start).Take(end - start));

            // Safe: <mark> injected AFTER encoding — cannot be abused
            context = Regex.Replace(context, Regex.Escape(encodedQuery),
                $"<mark>{encodedQuery}</mark>", RegexOptions.IgnoreCase);

            // Wrap Arabic/Urdu script runs in a styled RTL span
            context = Regex.Replace(context,
                @"[\u0600-\u06FF\u0750-\u077F\uFB50-\uFDFF\uFE70-\uFEFF]+",
                m => $"<span class=\"special\" dir=\"rtl\">{m.Value}</span>");

            return context;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
