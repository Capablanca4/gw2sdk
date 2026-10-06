using System.Drawing;

using GuildWars2;
using GuildWars2.Hero.Equipment.Dyes;

using HttpClientFactoryUsage.Samples.MemoryCache;
using HttpClientFactoryUsage.Samples.Polly;
using HttpClientFactoryUsage.Samples.RateLimit;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using Pastel;

HostApplicationBuilder appBuilder = Host.CreateApplicationBuilder(args);

// Configure logging
appBuilder.Logging.AddSimpleConsole(options =>
{
    options.ColorBehavior = LoggerColorBehavior.Enabled;
    options.SingleLine = true;
});

// Set up the HTTP client factory
appBuilder.Services.AddHttpClient<Gw2Client>()
    // Add a memory cache to the HTTP client pipeline, so that repeated requests for the same data are served from memory
    .AddMemoryCache()
    // Add a rate limiter to the HTTP client pipeline, so that requests are throttled to avoid hitting the API rate limit
    .AddRateLimiting()
    // Add resiliency strategies to the HTTP client pipeline using Polly
    .AddPolly()
    // Add resiliency strategies to the HTTP client pipeline using the standard resilience handler,
    // which is a simplified version of the Polly-based resiliency strategies
    .AddStandardResilienceHandler();

IHost app = appBuilder.Build();

// Obtain a Gw2Client from the service provider, which uses the HTTP client factory
Gw2Client gw2 = app.Services.GetRequiredService<Gw2Client>();

// Some demo code to print dye colors, using Pastel to colorize the console output
foreach (DyeColor dye in await gw2.Hero.Equipment.Dyes.GetColors().ValueOnly().ConfigureAwait(false))
{
    PrintColor(dye.Name, dye.Cloth.Rgb, dye.Leather.Rgb, dye.Metal.Rgb);
}

// Helper method to print a row of colors
void PrintColor(string name, Color cloth, Color leather, Color metal)
{
    app.Services.GetRequiredService<ILogger<DyeColor>>()
        .LogInformation(
            "{Name,-20} {Cloth,-25} {Leather,-25} {Metal,-25}",
            name,
            "  C  ".Pastel(Invert(cloth)).PastelBg(cloth),
            "  L  ".Pastel(Invert(leather)).PastelBg(leather),
            "  M  ".Pastel(Invert(metal)).PastelBg(metal)
        );

    static Color Invert(Color color)
    {
        return Color.FromArgb(color.ToArgb() ^ 0xffffff);
    }
}
