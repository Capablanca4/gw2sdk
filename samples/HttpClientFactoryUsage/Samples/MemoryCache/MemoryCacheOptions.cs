namespace HttpClientFactoryUsage.Samples.MemoryCache;

internal sealed class MemoryCacheOptions
{
    public TimeSpan Ttl { get; set; } = TimeSpan.FromHours(6);

    /// <summary>
    /// Which requests may be cached. Default: GET requests that carry no credentials, i.e. public
    /// game data. Account data is therefore never cached unless you opt in.
    /// </summary>
    public Func<HttpRequestMessage, bool> ShouldCache { get; set; } =
        request => request.Method == HttpMethod.Get && request.Headers.Authorization is null;
}
