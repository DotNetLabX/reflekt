// Note: Stays in Reflekt.API — carries Reflekt-specific policy (5s timeout, SSRF ranges).
// Extract to BuildingBlocks.Api if a second service needs SSRF-guarded HTTP fetching.
using AngleSharp;

namespace Reflekt.API.OpenGraph;

public sealed record FetchResult(bool Ok, string? Title, string? Description, string? ImageUrl, string? IconUrl)
{
    public static readonly FetchResult Failed = new(false, null, null, null, null);
}

public sealed class OpenGraphFetcher(IHttpClientFactory httpClientFactory)
{
    private static readonly AngleSharp.IConfiguration _angleSharpConfig = Configuration.Default;

    public async Task<FetchResult> FetchAsync(string url, CancellationToken ct = default)
    {
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return FetchResult.Failed;

            if (uri.Scheme is not ("http" or "https"))
                return FetchResult.Failed;

            var client = httpClientFactory.CreateClient("opengraph");
            using var response = await client.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
                return FetchResult.Failed;

            var html = await response.Content.ReadAsStringAsync(ct);
            return await ParseOpenGraphAsync(html);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return FetchResult.Failed;
        }
    }

    private static async Task<FetchResult> ParseOpenGraphAsync(string html)
    {
        using var context = BrowsingContext.New(_angleSharpConfig);
        using var document = await context.OpenAsync(req => req.Content(html));

        var title = document.QuerySelector("meta[property='og:title']")?.GetAttribute("content")
            ?? document.QuerySelector("title")?.TextContent;
        var description = document.QuerySelector("meta[property='og:description']")?.GetAttribute("content");
        var image = document.QuerySelector("meta[property='og:image']")?.GetAttribute("content");
        var icon = document.QuerySelector("link[rel='icon']")?.GetAttribute("href")
            ?? document.QuerySelector("link[rel='shortcut icon']")?.GetAttribute("href");

        return new FetchResult(true, title, description, image, icon);
    }
}
