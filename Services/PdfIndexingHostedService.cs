using System.Text.Json;

namespace MalfuzatExplorer.Services;

/// <summary>
/// A hosted background service that runs once when the application starts.
/// 
/// WHY BackgroundService?
///   IHostedService lets us do expensive startup work without blocking the web
///   server. The app becomes available immediately; semantic search is simply
///   unavailable until the index is ready (we show a banner in the UI).
///
/// STARTUP FLOW:
///   1. If Gemini API key is missing  → log a warning and exit early.
///   2. If a valid disk cache exists  → load it and skip all API calls.
///   3. Otherwise                     → call the API, build index, save cache.
/// </summary>
public sealed class PdfIndexingHostedService : BackgroundService
{
    private static readonly string[] PdfFiles =
    [
        "Malfuzat-1.pdf", "Malfuzat-2.pdf", "Malfuzat-3.pdf",
        "Malfuzat-4.pdf", "Malfuzat-7.pdf", "Malfuzat-8.pdf",
        "Malfuzat-9.pdf", "Malfuzat-10.pdf"
    ];

    // Cache file lives next to the PDFs in wwwroot/Malfuzat/
    private const string CacheFileName = "embedding-2-cache.json";

    private readonly PdfIndexer _indexer;
    private readonly VectorIndexService _vectorIndex;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<PdfIndexingHostedService> _logger;

    public PdfIndexingHostedService(
        PdfIndexer indexer,
        VectorIndexService vectorIndex,
        IWebHostEnvironment env,
        ILogger<PdfIndexingHostedService> logger)
    {
        _indexer = indexer;
        _vectorIndex = vectorIndex;
        _env = env;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // ── Guard: skip everything if the API key is not configured ───────────
        if (!_indexer.IsEmbedConfigured)
        {
            _logger.LogWarning(
                "⚠️  Gemini API key not configured — semantic search disabled. " +
                "Set Gemini:ApiKey in appsettings or user-secrets to enable it.");
            return;
        }

        // Small delay so the web server is fully up before we start heavy I/O
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        _logger.LogInformation("🔍 Starting PDF semantic indexing …");

        string pdfDir   = Path.Combine(_env.WebRootPath, "Malfuzat");
        string cachePath = Path.Combine(pdfDir, CacheFileName);

        // ── Try to restore from disk cache first (avoids all API calls) ───────
        var cached = await TryLoadCacheAsync(cachePath, pdfDir);
        if (cached is not null)
        {
            _vectorIndex.Build(cached);
            _logger.LogInformation(
                "✅ Semantic search ready — {Count} chunks loaded from disk cache.",
                cached.Count);
            return;
        }

        // ── No valid cache — run full indexing via Gemini API ─────────────────
        var allChunks = new List<ChunkResult>();

        foreach (string fileName in PdfFiles)
        {
            if (stoppingToken.IsCancellationRequested) break;

            string path = Path.Combine(pdfDir, fileName);
            if (!File.Exists(path))
            {
                _logger.LogWarning("PDF not found, skipping: {Path}", path);
                continue;
            }

            string volume = Path.GetFileNameWithoutExtension(fileName); // "Malfuzat-3"

            try
            {
                var chunks = await _indexer.IndexFileAsync(path, volume, stoppingToken);
                allChunks.AddRange(chunks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing {Volume}", volume);
            }
        }

        // Publish the finished index — after this, VectorIndexService.IsReady = true
        _vectorIndex.Build(allChunks);

        _logger.LogInformation(
            "✅ Semantic search ready — {Count} total chunks across all volumes.",
            allChunks.Count);

        // ── Persist to disk so subsequent startups load instantly ─────────────
        await SaveCacheAsync(cachePath, allChunks);
    }

    // ── Cache helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Loads the embedding cache from disk if it exists AND is newer than all PDFs.
    /// Returns null when the cache should be ignored (missing, corrupt, or stale).
    /// </summary>
    private async Task<List<ChunkResult>?> TryLoadCacheAsync(string cachePath, string pdfDir)
    {
        if (!File.Exists(cachePath)) return null;

        try
        {
            // Invalidate the cache when any PDF has been modified since we wrote it
            DateTime cacheTime = File.GetLastWriteTimeUtc(cachePath);

            bool pdfNewerThanCache = PdfFiles
                .Select(f => Path.Combine(pdfDir, f))
                .Where(File.Exists)
                .Any(f => File.GetLastWriteTimeUtc(f) > cacheTime);

            if (pdfNewerThanCache)
            {
                _logger.LogInformation(
                    "📄 PDF files changed since last index — cache invalidated, re-indexing.");
                return null;
            }

            await using var stream = File.OpenRead(cachePath);
            var chunks = await JsonSerializer.DeserializeAsync<List<ChunkResult>>(stream,
                cancellationToken: CancellationToken.None);

            if (chunks is { Count: > 0 })
            {
                _logger.LogInformation(
                    "💾 Cache hit — {Count} chunks loaded from {Path}", chunks.Count, cachePath);
                return chunks;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load embedding cache — will re-index.");
        }

        return null;
    }

    /// <summary>
    /// Serializes all embedded chunks to disk so the next startup can skip the API.
    /// Failure is non-fatal — the in-memory index is already live.
    /// </summary>
    private async Task SaveCacheAsync(string cachePath, List<ChunkResult> chunks)
    {
        try
        {
            await using var stream = File.Create(cachePath);
            await JsonSerializer.SerializeAsync(stream, chunks);
            _logger.LogInformation("💾 Embedding cache saved → {Path}", cachePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to save embedding cache — next restart will re-index from API.");
        }
    }
}
