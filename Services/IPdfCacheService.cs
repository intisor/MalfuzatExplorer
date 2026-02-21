namespace MalfuzatExplorer.Services
{
    public interface IPdfCacheService
    {
        /// <summary>
        /// Gets all cached page texts for a specific PDF file
        /// </summary>
        /// <param name="pdfFileName">Name of the PDF file (e.g., "Malfuzat-1.pdf")</param>
        /// <returns>Array of page texts, or null if file not found</returns>
        string[]? GetPages(string pdfFileName);

        /// <summary>
        /// Gets all cached PDF file names
        /// </summary>
        IEnumerable<string> GetAllPdfFileNames();

        /// <summary>
        /// Indicates whether the cache has been fully loaded
        /// </summary>
        bool IsLoaded { get; }
    }
}
