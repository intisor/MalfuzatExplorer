using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace MalfuzatExplorer.Services;

/// <summary>
/// Reads all PDF volumes with iText7, splits each page into overlapping text
/// chunks, and calls GeminiEmbeddingService to embed each chunk.
/// 
/// CHUNKING STRATEGY:
///   Each page of text is split into words. We slide a window of ChunkSize
///   words across the page, advancing by Stride words each step. Overlap
///   (ChunkSize - Stride) ensures that sentences crossing chunk boundaries
///   are represented in both adjacent chunks.
/// 
///   ChunkSize = 300 words  ≈ 400 tokens  (well within Gemini's 2 048 limit)
///   Stride    = 250 words  → 50-word overlap between adjacent chunks
/// </summary>
public sealed class PdfIndexer
{
    private const int ChunkSize = 300;
    private const int Stride = 250;

    // Delay between embedding API calls to respect the free-tier rate limit
    // (1 500 RPM on free tier → ~40 ms between calls is safe at our throughput)
    private static readonly TimeSpan ApiDelay = TimeSpan.FromMilliseconds(50);

    private readonly GeminiEmbeddingService _embed;
    private readonly ILogger<PdfIndexer> _logger;

    public PdfIndexer(GeminiEmbeddingService embed, ILogger<PdfIndexer> logger)
    {
        _embed = embed;
        _logger = logger;
    }

    /// <summary>
    /// True when the underlying Gemini API key is configured.
    /// Used by <see cref="PdfIndexingHostedService"/> to short-circuit at startup.
    /// </summary>
    public bool IsEmbedConfigured => _embed.IsConfigured;

    /// <summary>
    /// Indexes a single PDF file. Returns all embedded chunks for that volume.
    /// pdfPath  — absolute path to the PDF file
    /// volume   — friendly name like "Malfuzat-3"
    /// </summary>
    public async Task<List<ChunkResult>> IndexFileAsync(
        string pdfPath, string volume, CancellationToken ct = default)
    {
        var results = new List<ChunkResult>();

        _logger.LogInformation("Indexing {Volume} …", volume);

        using var reader = new PdfReader(pdfPath);
        using var document = new PdfDocument(reader);

        int totalPages = document.GetNumberOfPages();

        for (int pageNum = 1; pageNum <= totalPages && !ct.IsCancellationRequested; pageNum++)
        {
            string pageText = PdfTextExtractor.GetTextFromPage(document.GetPage(pageNum));
            if (string.IsNullOrWhiteSpace(pageText)) continue;

            // Split page into words (preserves Urdu whitespace-delimited tokens)
            string[] words = pageText.Split(
                [' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);

            // Slide the window across the words
            for (int start = 0; start < words.Length; start += Stride)
            {
                // LEARNING: Math.Min prevents going past the end of the array
                string[] windowWords = words[start..Math.Min(start + ChunkSize, words.Length)];

                // Skip tiny trailing chunks (less than 10 words = not useful)
                if (windowWords.Length < 10) break;

                string chunkText = string.Join(" ", windowWords);

                try
                {
                    // Call the Gemini API — this is the step that converts text → float[]
                    float[] embedding = await _embed.EmbedAsync(chunkText, "RETRIEVAL_DOCUMENT");

                    results.Add(new ChunkResult(volume, pageNum, chunkText, embedding));

                    // Respect rate limit between calls
                    await Task.Delay(ApiDelay, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Failed to embed chunk at {Volume} page {Page} word {Start}",
                        volume, pageNum, start);
                    // Skip failed chunk — don't abort the whole volume
                }
            }

            if (pageNum % 10 == 0)
                _logger.LogInformation("  {Volume}: {Page}/{Total} pages indexed",
                    volume, pageNum, totalPages);
        }

        _logger.LogInformation("Finished {Volume} — {Count} chunks", volume, results.Count);
        return results;
    }
}
