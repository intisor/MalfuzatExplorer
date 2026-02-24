using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Order;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System.Text.RegularExpressions;

namespace MalfuzatExplorer.Benchmarks;

/// <summary>
/// Compares the original sequential search (v1) against the
/// parallel Task.WhenAll search (v2) on the real Malfuzat PDF corpus.
///
/// Run with:  dotnet run -c Release
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[SimpleJob(RunStrategy.Monitoring, warmupCount: 1, iterationCount: 5, id: "MonitoringJob")]
public class SearchBenchmarks
{
    // ── Resolve PDF paths relative to the benchmark binary's output dir ────
    // The benchmark project lives in  <root>/benchmarks/
    // The PDFs live in                <root>/wwwroot/Malfuzat/
    private static readonly string PdfDir = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "wwwroot", "Malfuzat"));

    private static readonly string[] PdfFileNames =
    {
        "Malfuzat-1.pdf", "Malfuzat-2.pdf", "Malfuzat-3.pdf",
        "Malfuzat-4.pdf", "Malfuzat-7.pdf", "Malfuzat-8.pdf",
        "Malfuzat-9.pdf", "Malfuzat-10.pdf"
    };

    // Term likely to appear in several volumes — change if needed
    [Params("حضرت", "مسجد")]
    public string Query { get; set; } = "حضرت";

    // ════════════════════════════════════════════════════════════════════════
    // V1 — BEFORE: sequential foreach, re-reads every PDF on every request
    // ════════════════════════════════════════════════════════════════════════
    [Benchmark(Baseline = true, Description = "v1 Sequential (before)")]
    public async Task<List<string>> OldSequential()
    {
        var results = new List<string>();

        foreach (var fileName in PdfFileNames)
        {
            string pdfPath = Path.Combine(PdfDir, fileName);
            if (!File.Exists(pdfPath)) continue;

            using var reader   = new PdfReader(pdfPath);
            using var document = new PdfDocument(reader);

            for (int i = 1; i <= document.GetNumberOfPages(); i++)
            {
                string pageText = PdfTextExtractor.GetTextFromPage(document.GetPage(i));
                if (pageText.Contains(Query, StringComparison.OrdinalIgnoreCase))
                {
                    // Original code awaited a Task.Run-wrapped string op here
                    string context = await GetContextOldAsync(pageText, Query);
                    results.Add(
                        $"Found in {Path.GetFileNameWithoutExtension(pdfPath)} on leaf {i}: {context}");
                }
            }
        }

        // Original code then looped again to call SpecialLanguageAsync
        for (int i = 0; i < results.Count; i++)
            results[i] = await SpecialLanguageOldAsync(results[i], Query);

        return results;
    }

    // ════════════════════════════════════════════════════════════════════════
    // V2 — AFTER: parallel Task.WhenAll across all 8 volumes
    // ════════════════════════════════════════════════════════════════════════
    [Benchmark(Description = "v2 Parallel Task.WhenAll (after)")]
    public async Task<List<string>> NewParallel()
    {
        var tasks = PdfFileNames
            .Select(f => Path.Combine(PdfDir, f))
            .Where(File.Exists)
            .Select(path => SearchSinglePdfAsync(path, Query));

        var resultSets = await Task.WhenAll(tasks);
        var rawResults = resultSets.SelectMany(r => r).ToList();

        // Parallel post-processing
        var processed = await Task.WhenAll(
            rawResults.Select(r => SpecialLanguageNewAsync(r, Query))
        );

        return [.. processed];
    }

    // ── V1 helpers (faithful reproduction of original bugs included) ────────

    private static async Task<string> GetContextOldAsync(string content, string query)
    {
        return await Task.Run(() =>
        {
            query = query.Trim();
            string[] words = content.Split(
                new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            int idx = Array.FindIndex(words,
                w => w.Contains(query, StringComparison.OrdinalIgnoreCase));
            if (idx < 0) return "Query not found";
            int start = Math.Max(0, idx - 100);
            int end   = Math.Min(words.Length, idx + 100 + query.Length);
            string result = string.Join(" ", words.Skip(start).Take(end - start));
            return result;
        });
    }

    private static async Task<string> HighlightOldAsync(string text, string query) =>
        await Task.Run(() =>
            Regex.Replace(text, Regex.Escape(query), $"<mark>{query}</mark>",
                RegexOptions.IgnoreCase));

    // BUG reproduced: HighlightOld called both inside GetContext AND inside
    // SpecialLanguage, resulting in double-wrapping <mark> tags
    private static async Task<string> SpecialLanguageOldAsync(string result, string query)
    {
        result = await HighlightOldAsync(result, query);   // ← applies highlight AGAIN
        string pattern = @"[\u0600-\u06FF\u0750-\u077F\uFB50-\uFDFF\uFE70-\uFEFF]+";
        return Regex.Replace(result, pattern,
            m => $"<span class=\"special\" dir=\"rtl\">{m.Value}</span>");
    }

    // ── V2 helpers (refactored) ─────────────────────────────────────────────

    private static readonly Regex _arabicRegex =
        new(@"[\u0600-\u06FF\u0750-\u077F\uFB50-\uFDFF\uFE70-\uFEFF]+",
            RegexOptions.Compiled);

    private static Task<List<string>> SearchSinglePdfAsync(string pdfPath, string query) =>
        Task.Run(() =>
        {
            var results = new List<string>();
            using var reader   = new PdfReader(pdfPath);
            using var document = new PdfDocument(reader);

            for (int i = 1; i <= document.GetNumberOfPages(); i++)
            {
                string pageText = PdfTextExtractor.GetTextFromPage(document.GetPage(i));
                if (pageText.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    string context = GetContextNew(pageText, query);
                    results.Add(
                        $"Found in {Path.GetFileNameWithoutExtension(pdfPath)} on leaf {i}: {context}");
                }
            }
            return results;
        });

    private static string GetContextNew(string content, string query)
    {
        query = query.Trim();
        string[] words = content.Split(
            new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        int idx = Array.FindIndex(words,
            w => w.Contains(query, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return "Query not found";
        int start = Math.Max(0, idx - 100);
        int end   = Math.Min(words.Length, idx + 100 + query.Length);
        return string.Join(" ", words.Skip(start).Take(end - start));
    }

    private static Task<string> SpecialLanguageNewAsync(string result, string query) =>
        Task.Run(() =>
        {
            // Single highlight pass, then Arabic RTL wrap — no double-mark
            string highlighted = Regex.Replace(result, Regex.Escape(query),
                $"<mark>{query}</mark>", RegexOptions.IgnoreCase);
            return _arabicRegex.Replace(highlighted,
                m => $"<span class=\"special\" dir=\"rtl\">{m.Value}</span>");
        });
}
