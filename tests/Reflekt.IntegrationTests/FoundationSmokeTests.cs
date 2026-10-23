using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Reflekt.IntegrationTests.Helpers;

namespace Reflekt.IntegrationTests;

public class FoundationSmokeTests : IAsyncLifetime
{
    private readonly string _testDbPath =
        Path.Combine(Path.GetTempPath(), $"test-reflekt-{Guid.NewGuid():N}.db");

    private WebApplicationFactory<Program> _factory = null!;

    public Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UsePlaceholderGoogleCredentials();
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Reflekt"] = $"Data Source={_testDbPath}"
                    });
                });
            });
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Application_Starts()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        SqliteConnection.ClearAllPools();
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                if (File.Exists(_testDbPath)) File.Delete(_testDbPath);
                break;
            }
            catch (IOException) when (attempt < 20)
            {
                await Task.Delay(100);
                SqliteConnection.ClearAllPools();
            }
        }
    }
}
