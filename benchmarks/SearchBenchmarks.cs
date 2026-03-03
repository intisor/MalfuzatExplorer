using BenchmarkDotNet.Attributes;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace MalfuzatExplorer.Benchmarks;

/// <summary>
/// Compares the full search pipeline of the v1 (sequential foreach) and v2 (Task.WhenAll)
/// implementations against the real Malfuzat PDF corpus.
///
/// Both methods open, parse, and scan every PDF from disk each iteration so the measurement
/// includes I/O — the primary bottleneck the refactor was designed to address.
/// Returning int (match count) rather than List&lt;string&gt; eliminates allocation noise
/// from string building, keeping the focus on the search strategy difference.
///
/// Run with: dotnet run -c Release --project benchmarks
/// </summary>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
[BenchmarkCategory("Search", "PDF")]
public class SearchBenchmarks
{
    [Params(
        "\u062d\u0636\u0631\u062a",   // Arabic/Urdu — very frequent, worst-case match count
        "\u0645\u0633\u062c\u062f",   // Arabic/Urdu — moderately frequent
        "Islam"                        // English — low-frequency Latin-script term
    )]
    public string Query { get; set; } = string.Empty;

    private string[] _pdfPaths = [];

    [GlobalSetup]
    public void Setup()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "wwwroot", "Malfuzat")))
            dir = dir.Parent;

        if (dir is null)
            throw new DirectoryNotFoundException(
                "Could not locate 'wwwroot/Malfuzat'. Ensure PDFs are present in the main project.");

        string[] fileNames =
        [
            "Malfuzat-1.pdf", "Malfuzat-2.pdf", "Malfuzat-3.pdf",
            "Malfuzat-4.pdf", "Malfuzat-7.pdf", "Malfuzat-8.pdf",
            "Malfuzat-9.pdf", "Malfuzat-10.pdf",
        ];

        _pdfPaths = fileNames
            .Select(f => Path.Combine(dir.FullName, "wwwroot", "Malfuzat", f))
            .Where(File.Exists)
            .ToArray();
    }

    /// <summary>
    /// v1 (before) — opens each volume one at a time, awaiting completion before
    /// moving to the next. Equivalent to the original sequential foreach in HomeController.
    /// </summary>
    [Benchmark(Baseline = true, Description = "v1 Sequential foreach (before)")]
    public async Task<int> SequentialSearch()
    {
        int total = 0;
        foreach (var path in _pdfPaths)
        {
            total += await Task.Run(() =>
            {
                int found = 0;
                using var reader   = new PdfReader(path);
                using var document = new PdfDocument(reader);
                for (int i = 1; i <= document.GetNumberOfPages(); i++)
                {
                    if (PdfTextExtractor.GetTextFromPage(document.GetPage(i))
                            .Contains(Query, StringComparison.OrdinalIgnoreCase))
                        found++;
                }
                return found;
            });
        }
        return total;
    }

    /// <summary>
    /// v2 (after) — all volumes searched concurrently; Task.WhenAll waits for the
    /// slowest single volume rather than the sum of all volumes.
    /// </summary>
    [Benchmark(Description = "v2 Parallel Task.WhenAll (after)")]
    public async Task<int> ParallelSearch()
    {
        var tasks = _pdfPaths.Select(path => Task.Run(() =>
        {
            int found = 0;
            using var reader   = new PdfReader(path);
            using var document = new PdfDocument(reader);
            for (int i = 1; i <= document.GetNumberOfPages(); i++)
            {
                if (PdfTextExtractor.GetTextFromPage(document.GetPage(i))
                        .Contains(Query, StringComparison.OrdinalIgnoreCase))
                    found++;
            }
            return found;
        }));

        return (await Task.WhenAll(tasks)).Sum();
    }
}
