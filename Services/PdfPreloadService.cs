using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace MalfuzatExplorer.Services
{
    /// <summary>
    /// Background service that extracts all PDF text at application startup
    /// </summary>
    public class PdfPreloadService : IHostedService
    {
        private readonly PdfCacheService _cacheService;
        private readonly ILogger<PdfPreloadService> _logger;
        private readonly string[] _pdfPaths;

        public PdfPreloadService(PdfCacheService cacheService, ILogger<PdfPreloadService> logger, IWebHostEnvironment env)
        {
            _cacheService = cacheService;
            _logger = logger;

            // Same PDF paths from HomeController
            _pdfPaths = new string[]
            {
                Path.Combine(env.WebRootPath, "Malfuzat", "Malfuzat-1.pdf"),
                Path.Combine(env.WebRootPath, "Malfuzat", "Malfuzat-2.pdf"),
                Path.Combine(env.WebRootPath, "Malfuzat", "Malfuzat-3.pdf"),
                Path.Combine(env.WebRootPath, "Malfuzat", "Malfuzat-4.pdf"),
                Path.Combine(env.WebRootPath, "Malfuzat", "Malfuzat-7.pdf"),
                Path.Combine(env.WebRootPath, "Malfuzat", "Malfuzat-8.pdf"),
                Path.Combine(env.WebRootPath, "Malfuzat", "Malfuzat-9.pdf"),
                Path.Combine(env.WebRootPath, "Malfuzat", "Malfuzat-10.pdf")
            };
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting PDF preload into memory cache...");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            await Task.Run(() =>
            {
                foreach (var pdfPath in _pdfPaths)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    if (!File.Exists(pdfPath))
                    {
                        _logger.LogWarning("PDF not found: {Path}", pdfPath);
                        continue;
                    }

                    try
                    {
                        var pages = ExtractAllPages(pdfPath);
                        var fileName = Path.GetFileName(pdfPath);
                        _cacheService.SetPages(fileName, pages);
                        
                        _logger.LogInformation("Cached {FileName}: {PageCount} pages", fileName, pages.Length);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to cache {Path}", pdfPath);
                    }
                }

                _cacheService.MarkAsLoaded();
                stopwatch.Stop();
                _logger.LogInformation("PDF cache loaded in {Elapsed}ms", stopwatch.ElapsedMilliseconds);
            }, cancellationToken);

            return;
        }

        private string[] ExtractAllPages(string pdfPath)
        {
            using var reader = new PdfReader(pdfPath);
            using var document = new PdfDocument(reader);
            
            var pageCount = document.GetNumberOfPages();
            var pages = new string[pageCount];

            for (int i = 1; i <= pageCount; i++)
            {
                pages[i - 1] = PdfTextExtractor.GetTextFromPage(document.GetPage(i));
            }

            return pages;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("PDF preload service stopped");
            return Task.CompletedTask;
        }
    }
}
