using System.Net;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace HttpClientFactoryUsage.Samples.MemoryCache;

/// <summary>
/// Caches successful responses (status, headers such as X-Page-Total, and body) in an IMemoryCache.
/// Errors are never cached and pass through untouched. Known limitation: no stampede protection,
/// so concurrent identical misses each hit the network.
/// </summary>
internal sealed class MemoryCacheHandler(IMemoryCache cache, IOptions<MemoryCacheOptions> options)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        MemoryCacheOptions settings = options.Value;
        if (!settings.ShouldCache(request))
        {
            return await base.SendAsync(request, ct).ConfigureAwait(false);
        }

        string key = BuildKey(request);
        if (cache.TryGetValue(key, out CachedResponse? hit) && hit is not null)
        {
            return hit.ToResponse(request);
        }

        HttpResponseMessage response = await base.SendAsync(request, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return response;
        }

        try
        {
            CachedResponse captured = await CachedResponse.CaptureAsync(response, ct).ConfigureAwait(false);
            cache.Set(key, captured, settings.Ttl);
            return captured.ToResponse(request);
        }
        finally
        {
            response.Dispose();
        }
    }

    // URL (already contains ?v=schema) + language + a hash of the credentials, so users never share entries.
    private static string BuildKey(HttpRequestMessage request)
    {
        string? auth = request.Headers.Authorization?.ToString();
        string owner = auth is null
            ? "public"
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(auth)))[..16];

        return $"gw2|{owner}|{request.Headers.AcceptLanguage}|{request.RequestUri}";
    }

    private sealed class CachedResponse(
        HttpStatusCode status,
        byte[] body,
        KeyValuePair<string, string[]>[] headers,
        KeyValuePair<string, string[]>[] contentHeaders)
    {
        public static async Task<CachedResponse> CaptureAsync(HttpResponseMessage response, CancellationToken ct)
        {
            return new(response.StatusCode,
                await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false),
                [.. response.Headers
                    .Where(h => h.Key != "Transfer-Encoding")
                    .Select(h => new KeyValuePair<string, string[]>(h.Key, [.. h.Value]))],
                [.. response.Content.Headers
                    .Where(h => h.Key != "Content-Length")
                    .Select(h => new KeyValuePair<string, string[]>(h.Key, [.. h.Value]))]);
        }

        // A fresh message per hit: HttpResponseMessage content can only be read once.
        public HttpResponseMessage ToResponse(HttpRequestMessage request)
        {
            HttpResponseMessage response = new(status)
            {
                Content = new ByteArrayContent(body),
                RequestMessage = request
            };
            foreach ((string? name, string[]? values) in headers)
            {
                response.Headers.TryAddWithoutValidation(name, values);
            }

            foreach ((string? name, string[]? values) in contentHeaders)
            {
                response.Content.Headers.TryAddWithoutValidation(name, values);
            }

            return response;
        }
    }
}
