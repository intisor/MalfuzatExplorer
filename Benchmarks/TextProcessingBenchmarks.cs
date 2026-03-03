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
        "حضرت صاحب نے فرمایا کہ اسلام ایک کامل اور مکمل دین ہے جو انسانیت کی ہر ضرورت کو پورا کرتا ہے۔ " +
        "مسجد میں نماز پڑھنا اور اجتماعی عبادت کرنا مسلمانوں کے لیے بہت اہم ہے اور اس سے اتحاد پیدا ہوتا ہے۔ " +
        "قرآن کریم اللہ تعالیٰ کا کلام ہے جو حضرت محمد صلی اللہ علیہ وسلم پر نازل ہوا اور قیامت تک محفوظ ہے۔ " +
        "The Promised Messiah (peace be upon him) taught that true faith requires both belief and righteous action. " +
        "اللہ تعالیٰ کی رحمت اور مغفرت تمام گناہگاروں کے لیے کھلی ہے بشرطیکہ وہ سچے دل سے توبہ کریں۔ ";

    private static readonly string ShortText  = string.Concat(Enumerable.Repeat(BaseParagraph,  2));
    private static readonly string MediumText = string.Concat(Enumerable.Repeat(BaseParagraph,  8));
    private static readonly string LongText   = string.Concat(Enumerable.Repeat(BaseParagraph, 30));

    // Compiled once — mirrors the static readonly field in HomeController (v2).
    private static readonly Regex ArabicRegex =
        new(@"[\u0600-\u06FF\u0750-\u077F\uFB50-\uFDFF\uFE70-\uFEFF]+",
            RegexOptions.Compiled);

    [Params("Short", "Medium", "Long")]
    public string ContentSize { get; set; } = "Medium";

    [Params("حضرت", "مسجد")]
    public string Query { get; set; } = "حضرت";

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
