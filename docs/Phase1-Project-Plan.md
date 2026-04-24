# Phase 1: Core Stability & Security Audit (Malfuzat Explorer)
*Note: These tasks document the immediate architectural hardening completed during the sprint.*

- [ ] **Task 1: Eradicate Thread Pool Deadlocks**  
  Refactor `SearchPdfForQueryAsync`. Remove all `.Result` invocations. Convert CPU-bound text highlighters to synchronous methods. Implement `Task.WhenAll` for thread-safe file scanning.
  - **Category:** `🏗️ Portfolio Build`
  - **FUTA Course:** `SEN309`
  - **Content Output:** `📹 YouTube/Twitch`

- [ ] **Task 2: Neutralize XSS Injection Vectors**  
  Implement `HtmlEncoder.Default.Encode()` on all extracted PDF text before regex parsing. Secure the Razor views to prevent arbitrary script execution via `@Html.Raw()`.
  - **Category:** `🏗️ Portfolio Build`
  - **FUTA Course:** `SEN307`
  - **Content Output:** `💼 LinkedIn`

- [ ] **Task 3: Implement Defensive Pagination**  
  Extend `MalfuzatModel` with `PageNumber` and `PageSize`. Enforce server-side slicing to prevent unbounded response generation and OOM exceptions.
  - **Category:** `🏗️ Portfolio Build`
  - **FUTA Course:** `SEN303`
  - **Content Output:** `📝 Substack`

- [ ] **Task 4: Enforce IP Rate Limiting**  
  Register ASP.NET Core 8 `AddRateLimiter` middleware. Apply a `fixed-window` policy to the `/Home/Search` endpoint to aggressively throttle parallel scan abuse.
  - **Category:** `🏗️ Portfolio Build`
  - **FUTA Course:** `SEN307`
  - **Content Output:** `🐦 Twitter/X`

- [ ] **Task 5: Dependency Pruning & Health Probes**  
  Remove the unused 15MB `NEST` Elasticsearch dependency from the `.csproj`. Implement `IHealthCheck` to allow Azure load balancers to probe the `PdfCacheService` readiness state.
  - **Category:** `🏗️ Portfolio Build`
  - **FUTA Course:** `SEN208`
  - **Content Output:** `🚫 None`
