using MalfuzatExplorer.Services;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;

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
_ = Task.Run(async () =>
{
    using var scope = app.Services.CreateScope();
    var indexer = scope.ServiceProvider.GetRequiredService<MalfuzatIndexerService>();
    var searcher = scope.ServiceProvider.GetRequiredService<MalfuzatSearchService>();
    var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

    // 1. PHASE 4: Indexing
    string testPdfPath = System.IO.Path.Combine(env.WebRootPath, "Malfuzat", "Malfuzat-1.pdf");
    
    if (System.IO.File.Exists(testPdfPath))
    {
        System.Console.WriteLine("\n[Phase 4] Indexing Malfuzat-1.pdf...");
        try
        {
            var results = await indexer.ProcessAndEmbedBookAsync(testPdfPath, "Malfuzat-1");
            System.Console.WriteLine($"[SUCCESS] Embedded {results.Count} chunks.");

            // 2. PHASE 5: Search
            System.Console.WriteLine("\n[Phase 5] Initialising Search Engine...");
            searcher.Initialise(results);

            string testQuery = "What did the Promised Messiah say about prayer?";
            System.Console.WriteLine($"[Search] Testing query: \"{testQuery}\"");

            var searchResults = await searcher.SearchAsync(testQuery, topK: 3);
            
            foreach (var res in searchResults)
            {
                System.Console.WriteLine($" - [Score: {res.Score:P1}] Page {res.Chunk.Page}: {res.Chunk.Text.Substring(0, 50)}...");
            }
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"\n[ERROR] Pipeline failed: {ex.Message}");
        }
    }
});
// ==========================================

app.Run();
