using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Reflekt.IntegrationTests.Helpers;

namespace Reflekt.IntegrationTests;

public class AuthSmokeTests : IAsyncLifetime
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
    public async Task Me_WithoutCookie_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithoutCookie_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_RedirectsToGoogle()
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/auth/google/login");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.Host.Should().Be("accounts.google.com");
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
