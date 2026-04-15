using MalfuzatExplorer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// In-process result cache: stores up to 500 search result entries
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 500;
});

// ── Semantic Search Services ──────────────────────────────────────────────
// LEARNING: AddHttpClient<T> creates a managed HttpClient with connection
// pooling — much better than new HttpClient() which can exhaust sockets.
builder.Services.AddHttpClient<GeminiEmbeddingService>();

// Singleton: the in-memory vector index lives for the whole app lifetime
builder.Services.AddSingleton<VectorIndexService>();

// PdfIndexer is also singleton because it only does work during startup indexing
builder.Services.AddSingleton<PdfIndexer>();

// IHostedService: ASP.NET Core calls ExecuteAsync() automatically at startup
// The web server stays responsive; indexing happens in the background
builder.Services.AddHostedService<PdfIndexingHostedService>();
// ─────────────────────────────────────────────────────────────────────────

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
//app.MapControllerRoute(
//    name: "indexPdf",
//    pattern: "index-pdf",
//    defaults: new { controller = "Malfuzat", action = "IndexPdf" }
//);
app.Run();
