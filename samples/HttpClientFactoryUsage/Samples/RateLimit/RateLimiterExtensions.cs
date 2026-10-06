using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HttpClientFactoryUsage.Samples.RateLimit;

internal static class RateLimiterExtensions
{
    /// <summary>
    /// Client-side token-bucket limiter shared by every IGw2Client instance (including WithApiKey views).
    /// </summary>
    public static IHttpClientBuilder AddRateLimiting(
        this IHttpClientBuilder builder,
        Action<RateLimitOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.Configure<RateLimitOptions>(o => configure?.Invoke(o));
        builder.Services.TryAddSingleton<Gw2RateLimiter>();
        builder.Services.TryAddTransient<RateLimitHandler>();

        return builder.AddHttpMessageHandler<RateLimitHandler>();
    }
}
