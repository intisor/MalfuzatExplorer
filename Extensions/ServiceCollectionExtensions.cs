using MalfuzatExplorer.Services;

namespace MalfuzatExplorer.Extensions
{
    /// <summary>
    /// IServiceCollection extension methods for MalfuzatExplorer's DI registrations.
    /// Inspired by AmsaAPI's Extensions/ pattern — keeps Program.cs clean of
    /// service-layer concerns.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMalfuzatServices(this IServiceCollection services)
        {
            // PdfCacheService is registered as both its concrete type (so PdfPreloadService
            // can resolve it) and as the IPdfCacheService interface (for controller injection).
            services.AddSingleton<PdfCacheService>();
            services.AddSingleton<IPdfCacheService>(sp => sp.GetRequiredService<PdfCacheService>());
            services.AddHostedService<PdfPreloadService>();

            return services;
        }
    }
}
