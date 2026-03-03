using BenchmarkDotNet.Attributes;
using System.Text.RegularExpressions;

namespace MalfuzatExplorer.Benchmarks;

/// <summary>
/// Measures the CPU and allocation cost of the text-processing pipeline:
/// context extraction, query highlighting, and Arabic/RTL span wrapping.
/// No file I/O — all benchmarks run against in-memory sample content.
///
/// The static helpers are inlined here so the benchmark project has no
/// dependency on the web project's DI container.
/// </summary>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
[BenchmarkCategory("TextProcessing")]
public class TextProcessingBenchmarks
{
    private const string BaseParagraph =
        "\u062d\u0636\u0631\u062a \u0635\u0627\u062d\u0628 \u0646\u06d2 \u0641\u0631\u0645\u0627\u06cc\u0627 \u06a9\u06c1 \u0627\u0633\u0644\u0627\u0645 \u0627\u06cc\u06a9 \u06a9\u0627\u0645\u0644 \u0627\u0648\u0631 \u0645\u06a9\u0645\u0644 \u062f\u06cc\u0646 \u06c1\u06d2 \u062c\u0648 \u0627\u0646\u0633\u0627\u0646\u06cc\u062a \u06a9\u06cc \u06c1\u0631 \u0636\u0631\u0648\u0631\u062a \u06a9\u0648 \u067e\u0648\u0631\u0627 \u06a9\u0631\u062a\u0627 \u06c1\u06d2\u06d4 " +
        "\u0645\u0633\u062c\u062f \u0645\u06cc\u06ba \u0646\u0645\u0627\u0632 \u067e\u0691\u06be\u0646\u0627 \u0627\u0648\u0631 \u0627\u062c\u062a\u0645\u0627\u0639\u06cc \u0639\u0628\u0627\u062f\u062a \u06a9\u0631\u0646\u0627 \u0645\u0633\u0644\u0645\u0627\u0646\u0648\u06ba \u06a9\u06d2 \u0644\u06cc\u06d2 \u0628\u06c1\u062a \u0627\u06c1\u0645 \u06c1\u06d2\u06d4 " +
        "The Promised Messiah (peace be upon him) taught that true faith requires both belief and righteous action. " +
        "\u0627\u0644\u0644\u06c1 \u062a\u0639\u0627\u0644\u06b0\u06cc\u0670 \u06a9\u06cc \u0631\u062d\u0645\u062a \u0627\u0648\u0631 \u0645\u063a\u0641\u0631\u062a \u062a\u0645\u0627\u0645 \u06af\u0646\u0627\u06c1\u06af\u0627\u0631\u0648\u06ba \u06a9\u06d2 \u0644\u06cc\u06d2 \u06a9\u06be\u0644\u06cc \u06c1\u06d2\u06d4 ";

    private static readonly string ShortText  = string.Concat(Enumerable.Repeat(BaseParagraph,  2));
    private static readonly string MediumText = string.Concat(Enumerable.Repeat(BaseParagraph,  8));
    private static readonly string LongText   = string.Concat(Enumerable.Repeat(BaseParagraph, 30));

    // Compiled once — mirrors the static readonly field in HomeController (v2).
    private static readonly Regex ArabicRegex =
        new(@"[\u0600-\u06FF\u0750-\u077F\uFB50-\uFDFF\uFE70-\uFEFF]+",
            RegexOptions.Compiled);

    [Params("Short", "Medium", "Long")]
    public string ContentSize { get; set; } = "Medium";

    [Params("\u062d\u0636\u0631\u062a", "\u0645\u0633\u062c\u062f")]
    public string Query { get; set; } = "\u062d\u0636\u0631\u062a";

    private string _pageText = string.Empty;

    [GlobalSetup]
    public void Setup() =>
        _pageText = ContentSize switch
        {
            "Short" => ShortText,
            "Long"  => LongText,
            _       => MediumText,
        };

    /// <summary>
    /// Splits content into words, finds the query index, and extracts a 200-word window.
    /// </summary>
    [Benchmark(Baseline = true, Description = "GetContextAroundQuery")]
    public string GetContext() => GetContextAroundQuery(_pageText, Query);

    /// <summary>
    /// Full post-processing pipeline: highlight query, then wrap Arabic/Urdu runs in RTL span.
    /// </summary>
    [Benchmark(Description = "SpecialLanguage (highlight + RTL wrap)")]
    public string SpecialLanguage() => ApplySpecialLanguage(_pageText, Query);

    // ── Helpers inlined from HomeController (static, no DI needed) ──────────

    private static string GetContextAroundQuery(string content, string query)
    {
        query = query.Trim();
        string[] words = content.Split(
            [' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);

        int idx = Array.FindIndex(words,
            w => w.Contains(query, StringComparison.OrdinalIgnoreCase));

        if (idx < 0) return "Query not found";

        int start = Math.Max(0, idx - 100);
        int end   = Math.Min(words.Length, idx + 100 + query.Length);
        return string.Join(" ", words.Skip(start).Take(end - start));
    }

    private static string HighlightQuery(string text, string query) =>
        Regex.Replace(text, Regex.Escape(query),
            $"<mark>{query}</mark>", RegexOptions.IgnoreCase);

    private static string ApplySpecialLanguage(string result, string query)
    {
        string highlighted = HighlightQuery(result, query);
        return ArabicRegex.Replace(highlighted,
            m => $"<span class=\"special\" dir=\"rtl\">{m.Value}</span>");
    }
}
