using System.Threading.RateLimiting;

using Microsoft.Extensions.Options;

namespace HttpClientFactoryUsage.Samples.RateLimit;

/// <summary>Singleton holder so the bucket is shared across pooled handler instances.</summary>
internal sealed class Gw2RateLimiter : IDisposable
{
    public Gw2RateLimiter(IOptions<RateLimitOptions> options)
    {
        RateLimitOptions o = options.Value;
        Limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = o.RequestsPerMinute,                        // burst size
            TokensPerPeriod = Math.Max(1, o.RequestsPerMinute / 60), // refill per second
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            QueueLimit = o.QueueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true
        });
    }

    public RateLimiter Limiter { get; }   // <-- was RateLimiterExtensions
    public void Dispose()
    {
        Limiter.Dispose();
    }
}

internal sealed class RateLimitHandler(Gw2RateLimiter limiter) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        using RateLimitLease lease = await limiter.Limiter.AcquireAsync(1, ct).ConfigureAwait(false);
        if (!lease.IsAcquired)
        {
            TimeSpan? retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan ra) ? ra : null;
            throw new Exception("Client-side rate limit queue is full.");
        }
        return await base.SendAsync(request, ct).ConfigureAwait(false);
    }
}
