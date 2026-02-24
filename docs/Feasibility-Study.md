# Architectural Feasibility & Teardown Study: Malfuzat Explorer

## 1. The Concurrency Deadlock Fix (SEN309 - Operating Systems)

**The Previous Flaw:** In `HomeController.SearchPdfForQueryAsync`, `GetContextAroundQueryAsync(...).Result` was being invoked inside a `Parallel.ForEach` lambda. Because `.Result` blocks the current thread until the task completes, and `Parallel.ForEach` aggressively consumes thread pool threads, under concurrent user load, the server would exhaust its thread pool waiting for tasks that could not start (Classic Deadlock).

**The Intitech Execution:** We ripped out the `async` wrapper from `GetContextAroundQuery` (since Regex and string manipulation are pure CPU work, not I/O). By removing `.Result` and restructuring the parallelism using `Task.WhenAll`, we restored proper OS-level context switching.

---

## 2. The Injection Mitigation (SEN307 - Security)

**The Previous Flaw:** The engine highlighted text by using `Regex.Replace` to wrap the search query in `<span class="special">`, then pushed it straight to `@Html.Raw()` in Razor. If a user searched for `<script>alert(1)</script>`, it executed perfectly.

**The Intitech Execution:** We implemented a strict sanitization pipeline. User input and PDF context are explicitly HTML-encoded *before* our controlled highlighting tags are applied. This is a textbook defense-in-depth security implementation.

---

## 3. The Big O & Payload Optimization (SEN303 - DSA)

**The Previous Flaw:** The engine lacked pagination. A broad query would match thousands of pages. The server would allocate memory for 4,000+ strings, serialize them, and force the browser to render 4,000 DOM elements.

**The Intitech Execution:** By introducing stateful pagination models (`Take(20)`), we clamped the memory allocation ceiling. The server now processes in $O(N)$ but strictly returns an $O(1)$ payload size over the wire.
