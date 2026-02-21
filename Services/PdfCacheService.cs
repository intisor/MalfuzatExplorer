using System.Collections.Concurrent;

namespace MalfuzatExplorer.Services
{
    /// <summary>
    /// Thread-safe singleton cache storing extracted PDF page text in memory
    /// </summary>
    public class PdfCacheService : IPdfCacheService
    {
        private readonly ConcurrentDictionary<string, string[]> _cache = new();
        private volatile bool _isLoaded = false;

        public bool IsLoaded => _isLoaded;

        /// <summary>
        /// Stores all pages for a PDF file
        /// </summary>
        public void SetPages(string pdfFileName, string[] pages)
        {
            _cache[pdfFileName] = pages;
        }

        /// <summary>
        /// Marks the cache as fully loaded
        /// </summary>
        public void MarkAsLoaded()
        {
            _isLoaded = true;
        }

        public string[]? GetPages(string pdfFileName)
        {
            return _cache.TryGetValue(pdfFileName, out var pages) ? pages : null;
        }

        public IEnumerable<string> GetAllPdfFileNames()
        {
            return _cache.Keys;
        }
    }
}
