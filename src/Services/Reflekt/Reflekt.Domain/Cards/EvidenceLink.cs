namespace Reflekt.Domain.Cards;

public sealed class EvidenceLink : Entity
{
    public const int UrlMaxLength = 2048;
    public const int TitleMaxLength = 500;
    public const int DescriptionMaxLength = 1000;
    public const int ExternalKeyMaxLength = 50;

    private EvidenceLink() { }

    public required int CardId { get; init; }
    public required string Url { get; init; }
    public required EvidenceLinkSource Source { get; init; }
    public string? Title { get; private set; }
    public string? Description { get; private set; }
    public string? IconUrl { get; private set; }
    public string? ImageUrl { get; private set; }
    public string? ExternalKey { get; private set; }
    public DateTime? FetchedAt { get; private set; }
    public FetchStatus FetchStatus { get; private set; }

    internal static EvidenceLink Create(int cardId, string url, EvidenceLinkSource source, string? externalKey)
    {
        var link = new EvidenceLink
        {
            CardId = cardId,
            Url = url,
            Source = source,
            ExternalKey = externalKey,
        };

        if (source == EvidenceLinkSource.Jira)
        {
            link.FetchStatus = FetchStatus.Ok;
            link.FetchedAt = DateTime.UtcNow;
        }
        else
        {
            link.FetchStatus = FetchStatus.Pending;
        }

        return link;
    }

    internal void UpdateMetadata(string? title, string? description, string? iconUrl, string? imageUrl, FetchStatus status)
    {
        Title = title;
        Description = description;
        IconUrl = iconUrl;
        ImageUrl = imageUrl;
        FetchStatus = status;
        FetchedAt = DateTime.UtcNow;
    }
}
