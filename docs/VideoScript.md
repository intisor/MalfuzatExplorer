# Video Script — MalfuzatExplorer Performance Review

> **Format:** Talking-points walkthrough. Not word-for-word — use these as anchors and speak naturally.
> **Tone:** Senior dev explaining a real code review to another developer.
> **Screen layout:** Split-screen or tab-switch between `master` branch (before) and `experimenting-with-vectors` (after).

---

## Hook (1 min)

> "I built a search feature that scans 8 Arabic PDFs. On the first version it could take up to 28 seconds.
> After a refactor it consistently runs under 4 seconds cold — and under 5 milliseconds on repeat queries.
> I am going to show you every single change I made, explain why each one works, and show you the benchmark numbers."

Open GitHub, show the two branches. Say you will walk through `docs/Before-After-Comparison.md` as a guide.

---

## Section 1 — The Problem (3 min)

**Show:** The architecture diagram in the doc — "Before" section.

**Talking points:**
- The original `Search` action hit 8 PDF volumes **one at a time** inside a `foreach`.
- Each volume was read fresh off disk on **every single request**. No caching at all.
- `GetContextAroundQueryAsync` wrapped a tiny string operation in `Task.Run` — that is not async work, that is thread-pool pressure for nothing.
- There was also a double-highlight bug hidden in the call chain (covered in Section 5).

---

## Section 2 — Sequential → Parallel (5 min)

**Show the two implementations side by side (open both branches in GitHub):**

```
BEFORE (master branch — HomeController.cs):
  foreach (var pdfPath in pdfFiles)
  {
      await GetContextAroundQueryAsync(pageText, query);
  }

AFTER (experimenting-with-vectors — HomeController.cs):
  var tasks = _pdfFiles
      .Select(f => PdfPath(f))
      .Where(System.IO.File.Exists)
      .Select(path => SearchSinglePdfAsync(path, query));

  var resultSets = await Task.WhenAll(tasks);
```

**Talking points:**
- `foreach` + `await` inside the loop means: finish volume 1, THEN start volume 2.
  Total time = **sum** of all 8 volumes.
- `Task.WhenAll` fires all 8 at the same time.
  Total time = the **slowest single volume**, not the sum.
- This is **I/O-bound** work — reading PDFs from disk. I/O-bound is exactly where `Task.WhenAll` gives real gains.
  If this were pure CPU work, 2 physical cores cannot run 8 threads truly in parallel anyway.
- Each volume runs inside `Task.Run` because iText7 PDF parsing is synchronous —
  you push heavy synchronous work off the ASP.NET request thread onto the thread pool.

**Number to call out:** Cold start 28 ms → 22 ms. 21% faster on the very first request after boot.

---

## Section 3 — IMemoryCache (4 min)

**Show the before/after cache code:**

```
BEFORE:
  List<string> results = await SearchPdfForQueryAsync(model.Query);
  // full PDF scan on every single request

AFTER:
  string cacheKey = $"search::{model.Query.Trim().ToLowerInvariant()}";
  if (!_cache.TryGetValue(cacheKey, out List<string>? rawResults))
  {
      rawResults = await SearchPdfForQueryAsync(model.Query);
      _cache.Set(cacheKey, rawResults, new MemoryCacheEntryOptions
      {
          SlidingExpiration = TimeSpan.FromMinutes(30),
          Size = 1
      });
  }
```

**Talking points:**
- This corpus never changes at runtime. The same query always returns the same results.
  That is the textbook definition of a cacheable operation.
- `AddMemoryCache(o => o.SizeLimit = 500)` registered in `Program.cs`.
  `Size = 1` per entry — maximum 500 distinct queries cached at any time.
- `SlidingExpiration` — if nobody searches for that term for 30 minutes, the entry evicts itself.
  Memory is not held forever.
- The cache key normalises the query: trim whitespace, lowercase.
  `"حضرت "` (with trailing space) and `"حضرت"` hit the same cached slot.

**Number to call out:** Warm cache hit = under 5 ms. That is more than **99% faster** than the uncached path.
Not a rounding error — that is a full PDF scan avoided completely.

---

## Section 4 — Static Compiled Regex (3 min)

**Show the before/after:**

```
BEFORE:
  string pattern = @"[\u0600-\u06FF...]";
  Regex.Replace(input, pattern, match => ...);  // compiles a new DFA on every call

AFTER:
  private static readonly Regex _arabicRegex =
      new(@"[\u0600-\u06FF\u0750-\u077F\uFB50-\uFDFF\uFE70-\uFEFF]+",
          RegexOptions.Compiled);
```

**Talking points:**
- `Regex.Replace` with a plain string pattern compiles a **new DFA (deterministic finite automaton)** on every call.
  That compilation allocates memory and burns CPU each time — not free.
- `RegexOptions.Compiled` builds the DFA **once** at startup and emits IL so subsequent calls go straight to machine code.
- `static readonly` = one allocation for the **entire app lifetime**.
  Every request after that reuses the same compiled object.
- This matters here because `SpecialLanguageAsync` is called once **per search result**.
  A query that returns 40 matches triggered 40 Regex compilations in the old version.

---

## Section 5 — The Double Highlight Bug (4 min)

> "This is my favourite change because it was a real bug visible in the browser — and I only found it during the refactor."

**Show the old call chain step by step:**

```
// In Search() — caller called HighlightQueryAsync directly:
results[i] = await HighlightQueryAsync(results[i], model.Query);   // pass 1: adds <mark>

// Then passed the already-highlighted string into SpecialLanguageAsync...
results[i] = await SpecialLanguageAsync(results[i], model.Query);

// ...which internally called HighlightQueryAsync AGAIN:
public async Task<string> SpecialLanguageAsync(string result, string query)
{
    result = await HighlightQueryAsync(result, query);  // pass 2: adds <mark> inside existing <mark>
    ...
}
```

**Talking points:**
- `HighlightQueryAsync` wraps the query in `<mark>...</mark>`.
- The old code called it once from `Search()`, then passed the result into `SpecialLanguageAsync` which called it again.
- Output in the browser: `<mark><mark>حضرت</mark></mark>`.
  Some browsers render the inner mark as literal text. CSS only styles the outer tag.
- The fix is **ownership**: `SpecialLanguageAsync` owns the single highlight pass.
  No other caller touches `HighlightQuery` on the same string.

**Show the fix:**

```
public Task<string> SpecialLanguageAsync(string result, string query) =>
    Task.Run(() =>
    {
        string highlighted = HighlightQuery(result, query);  // once, here only
        return _arabicRegex.Replace(highlighted,
            m => $"<span class=\"special\" dir=\"rtl\">{m.Value}</span>");
    });
```

---

## Section 6 — Benchmark Numbers (4 min)

**Show:** The summary table from `docs/Before-After-Comparison.md`.

**Walk through the columns:**

| Column | What it tells you |
|---|---|
| Mean | Average time per iteration |
| StdDev | How consistent (predictable) the timing is |
| Ratio | How v2 compares to the v1 baseline (1.00 = same) |
| Allocated | Heap memory allocated per call |

**Key numbers to call out:**

1. **Warm OS cache, hot path:** v2 is 34% faster for a high-frequency Arabic query (0.93 ms vs 1.40 ms).
2. **StdDev is 4× lower on v2** (202 µs vs 420 µs for `حضرت`).
   v1 response times were unpredictable. v2 is tight and consistent.
   In a production search tool, **predictability matters as much as raw speed** — users notice inconsistency.
3. **Why the warm-path numbers are close:**
   After BenchmarkDotNet's warmup, the OS has all 8 PDFs in RAM.
   Both methods are then bottlenecked on in-memory iText7 parsing, not disk I/O.
   On a 2-physical-core laptop the thread pool cannot run 8 tasks truly in parallel.
4. **Where the parallel approach wins decisively:**
   - Cold start: 28 ms → 22 ms
   - IMemoryCache hit: 2-4 s → under 5 ms on repeat queries
   - Multi-user: 8 simultaneous users each get `Task.WhenAll` instead of a serial queue

**Allocation note:** v2 allocates 2.7 KB vs v1 2.1 KB per call (+28%).
That is the `Task` and `List` overhead from the parallel machinery — negligible given the latency savings,
and completely eliminated on cache hits.

---

## Outro (1 min)

> "Every one of these changes applies to any ASP.NET Core app that does expensive reads:
> Parallel I/O with Task.WhenAll. IMemoryCache for stable data.
> Compiled Regex as static readonly. Single ownership of each operation.
> The next video covers the `experimenting-with-vectors` branch —
> replacing this PDF text scan with vector embeddings and semantic search."

---

## Summary Slide

| Change | Impact |
|---|---|
| Sequential foreach → Task.WhenAll | Total time = slowest volume, not sum |
| IMemoryCache (30-min sliding) | Repeat queries under 5 ms (was 2-4 s) |
| static readonly Regex + Compiled | One DFA for app lifetime, not per call |
| Double-highlight bug removed | SpecialLanguageAsync owns the single pass |
| Unnecessary Task.Run on strings removed | Thread-pool pressure eliminated |
| IWebHostEnvironment injected | Path resolved once at startup, not per-request |
| MalfuzatModel.Query → string? | Nullability contract correct |
