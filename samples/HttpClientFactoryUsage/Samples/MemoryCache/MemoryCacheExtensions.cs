using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HttpClientFactoryUsage.Samples.MemoryCache;

internal static class MemoryCacheExtensions
{
    /// <summary>
    /// A plain DelegatingHandler registered with AddHttpMessageHandler. Register it BEFORE AddGw2Retry():
    /// it then sits outside retry and rate limiting, so a cache hit costs no retry and no rate-limit token.
    /// </summary>
    public static IHttpClientBuilder AddMemoryCache(
        this IHttpClientBuilder builder,
        Action<MemoryCacheOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddMemoryCache();
        builder.Services.Configure<MemoryCacheOptions>(o => configure?.Invoke(o));
        builder.Services.TryAddTransient<MemoryCacheHandler>();

        return builder.AddHttpMessageHandler<MemoryCacheHandler>();
    }
}
