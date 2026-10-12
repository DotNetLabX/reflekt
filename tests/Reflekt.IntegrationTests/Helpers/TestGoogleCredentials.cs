using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Reflekt.IntegrationTests.Helpers;

public static class TestGoogleCredentials
{
    // The Google handler validates its options at startup; the values never reach Google.
    public static IWebHostBuilder UsePlaceholderGoogleCredentials(this IWebHostBuilder builder) =>
        builder.ConfigureAppConfiguration((_, config) =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Google:ClientId"] = "test-client-id",
                ["Auth:Google:ClientSecret"] = "test-client-secret",
            }));
}
