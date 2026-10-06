namespace HttpClientFactoryUsage.Samples.RateLimit;


internal sealed class RateLimitOptions
{
    /// <summary>Conservative default. Check the API's documented rate limit and adjust.</summary>
    public int RequestsPerMinute { get; set; } = 300;

    /// <summary>Requests allowed to wait for a token; beyond this a Gw2RateLimitException is thrown.</summary>
    public int QueueLimit { get; set; } = 1000;
}
