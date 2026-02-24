# Product Requirements Document (PRD): Malfuzat Explorer
**Version:** 1.1 (Refactored Architecture)  
**Status:** Documented / Active  
**Date:** February 2026  

## 1. Executive Summary
Malfuzat Explorer is a hyper-optimized, in-memory full-text search engine for Ahmadiyya Islamic literature. Designed to bypass the heavy infrastructure costs of ElasticSearch or Azure Cognitive Search, the system parses thousands of PDF pages natively in memory. Recent architectural upgrades have transformed the system from a vulnerable, deadlock-prone prototype into a thread-safe, XSS-hardened, and rate-limited enterprise tool capable of sub-100ms searches across English, Arabic, and Urdu text.

## 2. Background & Market Opportunity
**The Engineering Gap:** Implementing full-text search over heavily formatted, multi-lingual PDFs usually requires massive infrastructure overhead (e.g., NEST/Elasticsearch clusters). For a student-led project running on limited Azure resources, this is financially unviable.  
**The Intitech Solution:**  
We execute a "sapa-driven" architecture: stripping out the unused 15MB NEST dependency, caching the PDFs in RAM via `ConcurrentDictionary`, and parallelizing the text extraction. The recent refactors strictly cap resource consumption to prevent the App Service from crashing under load.

## 3. Goals & Success Metrics
| Metric | Previous State | Current Target |
|---|---|---|
| Thread Pool Starvation | Deadlocks under heavy load | 0 deadlocks (100% async pipeline) |
| Security (XSS) | Vulnerable via `Html.Raw()` | 100% sanitized via `HtmlEncoder` |
| Max Response Size | Unbounded (4,000+ results) | Strictly paginated (Max 20 per page) |
| API Abuse Protection | 0 Rate Limiting | 10 req/min per IP |

## 4. User Personas

**Persona 1: The Theology Researcher 📖**
* **Goal:** Cross-reference specific Arabic/Urdu quotes across all 10 Malfuzat volumes instantly.
* **Pain:** Standard PDF readers require opening 10 separate files and hitting `Ctrl+F` 10 times.
* **Requirement:** A single unified search bar that highlights contextual matches perfectly, regardless of RTL (Right-to-Left) language complexities.

**Persona 2: The FUTA Student Developer (Auditor) 👨🏾‍💻**
* **Goal:** Study the codebase to understand concurrency and memory management.
* **Requirement:** Clean, documented code that demonstrates OS-level process scheduling and defensive programming.

## 5. Feature Specifications (The Refactored Core)

### F1 — XSS-Safe Multilingual Highlighter
**Summary:** Safely renders search terms in English, Arabic, and Urdu without executing malicious payloads.
* **Acceptance Criteria:**
  * Raw PDF text is intercepted and passed through `System.Text.Encodings.Web.HtmlEncoder.Default.Encode()`.
  * Contextual `<mark>` tags are injected *after* encoding via strict Regex.
  * Ensures zero Cross-Site Scripting (XSS) vulnerabilities on the `Index.cshtml` view.

### F2 — Thread-Safe Parallel Search Engine
**Summary:** Scans 10 volumes concurrently without destroying the ASP.NET Core Thread Pool.
* **Acceptance Criteria:**
  * `.Result` blocking calls are entirely eradicated from the `Parallel.ForEach` loops.
  * I/O vs CPU-bound tasks are clearly delineated. Pure CPU text-parsing methods are made synchronous, while file iteration uses `await Task.WhenAll()`.

### F3 — Defensive Unbounded Response Pagination
**Summary:** Protects the server's RAM and the client's browser from payload crashes.
* **Acceptance Criteria:**
  * The `MalfuzatModel` enforces a strict `PageSize = 20`.
  * The engine slices the `IEnumerable` before sending it over the wire, preventing a single query (like searching the letter "a") from crashing the Azure App Service.

### F4 — Fixed-Window Rate Limiting
**Summary:** Self-inflicted DoS protection.
* **Acceptance Criteria:**
  * ASP.NET Core 8 native `AddRateLimiter` configured for 10 requests per minute per IP.
  * Rejects overwhelming traffic instantly with HTTP 429 to protect the in-memory cache.
