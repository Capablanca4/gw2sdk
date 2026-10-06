using Microsoft.Extensions.DependencyInjection;

using Polly;

namespace HttpClientFactoryUsage.Samples.Polly;

internal static class ResilencyExtensions
{
    public static IHttpClientBuilder AddPolly(
        this IHttpClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddResilienceHandler(
            "Gw2Resiliency", // Pipeline name (used for logging)
            resiliencePipelineBuilder =>
            {
                // Scroll down to see the Gw2Resiliency class which holds the resiliency strategies
                resiliencePipelineBuilder
                // configure the total timeout for the entire request, including retries
                .AddTimeout(Gw2Resiliency.TotalTimeoutStrategy)
                // configure delayed retries for failed requests
                .AddRetry(Gw2Resiliency.RetryStrategy)
                // configure blocking requests after too many failures
                .AddCircuitBreaker(Gw2Resiliency.CircuitBreakerStrategy)
                // configure hedging for slow requests and for certain errors
                .AddHedging(Gw2Resiliency.HedgingStrategy)
                // configure the timeout for each individual request attempt
                .AddTimeout(Gw2Resiliency.AttemptTimeoutStrategy);
            });

        return builder;
    }
}
