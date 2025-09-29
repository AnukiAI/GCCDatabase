using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;

namespace GCCWebApp.Extensions
{
    public static class PerformanceExtensions
    {
        public static IServiceCollection AddPerformanceOptimization(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });

            services.Configure<BrotliCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;
            });

            services.Configure<GzipCompressionProviderOptions>(options =>
            {
                options.Level = CompressionLevel.Optimal;
            });

            services.AddMemoryCache();
            
            return services;
        }

        public static WebApplication UsePerformanceOptimization(this WebApplication app, IWebHostEnvironment? environment = null)
        {
            app.UseResponseCompression();
            return app;
        }

        public static IServiceCollection AddBundlingAndMinification(this IServiceCollection services, IWebHostEnvironment environment)
        {
            // Basic bundling and minification - can be expanded
            return services;
        }
    }
}