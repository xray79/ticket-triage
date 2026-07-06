using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Infrastructure.Caching;

public static class DependencyInjection
{
    /// <summary>
    /// Redis when a connection string is configured (real deployments); an in-memory
    /// IDistributedCache otherwise, so caching-dependent code works the same way in local
    /// dev or CI without standing up Redis just to run the app.
    /// </summary>
    public static IServiceCollection AddDistributedCaching(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Redis");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddStackExchangeRedisCache(options => options.Configuration = connectionString);
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        return services;
    }
}
