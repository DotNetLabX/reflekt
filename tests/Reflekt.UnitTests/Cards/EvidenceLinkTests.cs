using FluentAssertions;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Cards.Events;

namespace Reflekt.UnitTests.Cards;

public class EvidenceLinkTests
{
    private static Card CreateCard() =>
        Card.Create("Some text", sprintId: 1, participantId: 2, ColumnType.WentWell, CardType.Note, null);

    // AttachEvidenceLink

    [Fact]
    public void AttachEvidenceLink_GitHub_CreatesWithPendingStatus()
    {
        var card = CreateCard();
        var link = card.AttachEvidenceLink("https://github.com/org/repo", EvidenceLinkSource.GitHub, null);

        link.FetchStatus.Should().Be(FetchStatus.Pending);
        link.FetchedAt.Should().BeNull();
    }

    [Fact]
    public void AttachEvidenceLink_Jira_CreatesWithOkStatus()
    {
        var card = CreateCard();
        var link = card.AttachEvidenceLink("https://org.atlassian.net/browse/PROJ-123", EvidenceLinkSource.Jira, "PROJ-123");

        link.FetchStatus.Should().Be(FetchStatus.Ok);
        link.ExternalKey.Should().Be("PROJ-123");
        link.FetchedAt.Should().NotBeNull();
    }

    [Fact]
    public void AttachEvidenceLink_Generic_CreatesWithPendingStatus()
    {
        var card = CreateCard();
        var link = card.AttachEvidenceLink("https://example.com/page", EvidenceLinkSource.Generic, null);

        link.FetchStatus.Should().Be(FetchStatus.Pending);
    }

    [Fact]
    public void AttachEvidenceLink_RaisesEvidenceLinkAttachedEvent()
    {
        var card = CreateCard();
        card.ClearDomainEvents();

        card.AttachEvidenceLink("https://github.com/org/repo", EvidenceLinkSource.GitHub, null);

        card.DomainEvents.Should().HaveCount(1);
        var ev = card.DomainEvents[0].Should().BeOfType<EvidenceLinkAttachedEvent>().Subject;
        ev.Card.Should().BeSameAs(card);
        ev.EvidenceLink.Should().NotBeNull();
    }

    [Fact]
    public void AttachEvidenceLink_AddsToCollection()
    {
        var card = CreateCard();
        card.EvidenceLinks.Should().BeEmpty();

        card.AttachEvidenceLink("https://github.com/org/repo", EvidenceLinkSource.GitHub, null);

        card.EvidenceLinks.Should().HaveCount(1);
    }

    [Fact]
    public void AttachEvidenceLink_EmptyUrl_Throws()
    {
        var card = CreateCard();
        var act = () => card.AttachEvidenceLink("", EvidenceLinkSource.GitHub, null);

        act.Should().Throw<ArgumentException>();
    }

    // UpdateEvidenceLinkMetadata

    [Fact]
    public void UpdateMetadata_SetsAllFieldsAndRaisesEvent()
    {
        var card = CreateCard();
        var link = card.AttachEvidenceLink("https://github.com/org/repo", EvidenceLinkSource.GitHub, null);
        card.ClearDomainEvents();

        var before = DateTime.UtcNow;
        card.UpdateEvidenceLinkMetadata(link.Id, "Title", "Desc", "https://icon", "https://img", FetchStatus.Ok);
        var after = DateTime.UtcNow;

        var updated = card.EvidenceLinks[0];
        updated.Title.Should().Be("Title");
        updated.Description.Should().Be("Desc");
        updated.IconUrl.Should().Be("https://icon");
        updated.ImageUrl.Should().Be("https://img");
        updated.FetchStatus.Should().Be(FetchStatus.Ok);
        updated.FetchedAt.Should().NotBeNull().And.BeOnOrAfter(before).And.BeOnOrBefore(after);

        card.DomainEvents.Should().HaveCount(1);
        card.DomainEvents[0].Should().BeOfType<EvidenceLinkFetchedEvent>();
    }

    [Fact]
    public void UpdateMetadata_Failed_SetsFetchedAtAndStatus()
    {
        var card = CreateCard();
        var link = card.AttachEvidenceLink("https://github.com/org/repo", EvidenceLinkSource.GitHub, null);

        card.UpdateEvidenceLinkMetadata(link.Id, null, null, null, null, FetchStatus.Failed);

        var updated = card.EvidenceLinks[0];
        updated.FetchStatus.Should().Be(FetchStatus.Failed);
        updated.FetchedAt.Should().NotBeNull();
        updated.Title.Should().BeNull();
    }

    [Fact]
    public void UpdateMetadata_NonexistentLink_Throws()
    {
        var card = CreateCard();
        var act = () => card.UpdateEvidenceLinkMetadata(9999, null, null, null, null, FetchStatus.Failed);

        act.Should().Throw<InvalidOperationException>();
    }

    // DetachEvidenceLink

    [Fact]
    public void DetachEvidenceLink_RemovesFromCollection()
    {
        var card = CreateCard();
        var link = card.AttachEvidenceLink("https://github.com/org/repo", EvidenceLinkSource.GitHub, null);
        card.EvidenceLinks.Should().HaveCount(1);

        card.DetachEvidenceLink(link.Id);

        card.EvidenceLinks.Should().BeEmpty();
    }

    [Fact]
    public void DetachEvidenceLink_RaisesDetachedEvent()
    {
        var card = CreateCard();
        var link = card.AttachEvidenceLink("https://github.com/org/repo", EvidenceLinkSource.GitHub, null);
        card.ClearDomainEvents();

        card.DetachEvidenceLink(link.Id);

        card.DomainEvents.Should().HaveCount(1);
        var ev = card.DomainEvents[0].Should().BeOfType<EvidenceLinkDetachedEvent>().Subject;
        ev.Card.Should().BeSameAs(card);
        ev.EvidenceLinkId.Should().Be(link.Id);
    }

    [Fact]
    public void DetachEvidenceLink_NonexistentLink_Throws()
    {
        var card = CreateCard();
        var act = () => card.DetachEvidenceLink(9999);

        act.Should().Throw<InvalidOperationException>();
    }
}
