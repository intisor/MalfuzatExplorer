using MalfuzatExplorer.Services;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using System.Text.Json;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient<GeminiEmbeddingService>();
builder.Services.AddSingleton<PdfIndexer>();
builder.Services.AddScoped<MalfuzatIndexerService>();
builder.Services.AddSingleton<MalfuzatSearchService>();

// In-process result cache: stores up to 500 search result entries
builder.Services.AddMemoryCache(options =>
{
	options.SizeLimit = 500;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Home/Error");
	// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}");

// ==========================================
// PHASE 4 TEST SCRIPT (TEMPORARY)
// ==========================================
// This will run ONCE when you start the app.
// Make sure appsettings.Development.json has your Gemini API Key!
// ==========================================
// PHASE 4 & 5 TEST SCRIPT
// ==========================================
_ = Task.Run(async () =>
{
    using var scope = app.Services.CreateScope();
    var indexer = scope.ServiceProvider.GetRequiredService<MalfuzatIndexerService>();
    var searcher = scope.ServiceProvider.GetRequiredService<MalfuzatSearchService>();
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

    string testPdfPath = Path.Combine(env.WebRootPath, "Malfuzat", "Malfuzat-1.pdf");
    string vectorDbPath = Path.Combine(env.WebRootPath, "Malfuzat", "Malfuzat-1-vectors.json");

    List<ChunkResult> results = new();

    try
    {
        // 1. CHECK FOR EXISTING DATABASE
        if (File.Exists(vectorDbPath))
        {
            Console.WriteLine("\n[Phase 4] Found existing vector database. Loading from disk...");
            using FileStream openStream = File.OpenRead(vectorDbPath);
            results = await JsonSerializer.DeserializeAsync<List<ChunkResult>>(openStream) ?? new();
            Console.WriteLine($"[SUCCESS] Loaded {results.Count} chunks from JSON instantly.");
        }
        else if (File.Exists(testPdfPath))
        {
            // 2. NO DATABASE FOUND -> INDEX FROM SCRATCH
            Console.WriteLine("\n[Phase 4] No database found. Indexing Malfuzat-1.pdf...");
            results = await indexer.ProcessAndEmbedBookAsync(testPdfPath, "Malfuzat-1");
            Console.WriteLine($"[SUCCESS] Embedded {results.Count} chunks.");

            // 3. SAVE TO DISK FOR NEXT TIME
            Console.WriteLine("\n[Phase 4.5] Saving vectors to JSON...");
            using FileStream createStream = File.Create(vectorDbPath);
            await JsonSerializer.SerializeAsync(createStream, results);
            Console.WriteLine("[SUCCESS] Vector database saved to disk.");
        }

        // 4. INITIALIZE SEARCH AND TEST
        if (results.Any())
        {
            Console.WriteLine("\n[Phase 5] Initialising Search Engine...");
            searcher.Initialise(results);

            string testQuery = "What did the Promised Messiah say about prayer?";
            Console.WriteLine($"[Search] Testing query: \"{testQuery}\"");

            var searchResults = await searcher.SearchAsync(testQuery, topK: 3);
            
            foreach (var res in searchResults)
            {
                string preview = res.Chunk.Text.Length > 50 
                    ? res.Chunk.Text.Substring(0, 50) + "..." 
                    : res.Chunk.Text;

                Console.WriteLine($" - [Score: {res.Score:P1}] Page {res.Chunk.Page}: {preview}");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n[ERROR] Pipeline failed: {ex.Message}");
    }
});
// ==========================================

app.Run();
