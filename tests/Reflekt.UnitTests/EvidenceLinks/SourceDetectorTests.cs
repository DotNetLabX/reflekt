using FluentAssertions;
using Reflekt.API.Features.EvidenceLinks;
using Reflekt.Domain.Cards;

namespace Reflekt.UnitTests.EvidenceLinks;

public class SourceDetectorTests
{
    [Fact]
    public void Detect_GitHubUrl_ReturnsGitHub()
    {
        var (source, key) = EvidenceLinkSourceDetector.Detect("https://github.com/org/repo/pull/1", null);

        source.Should().Be(EvidenceLinkSource.GitHub);
        key.Should().BeNull();
    }

    [Fact]
    public void Detect_JiraBrowseUrl_ReturnsJiraWithKey()
    {
        var (source, key) = EvidenceLinkSourceDetector.Detect("https://anything.com/browse/PROJ-123", null);

        source.Should().Be(EvidenceLinkSource.Jira);
        key.Should().Be("PROJ-123");
    }

    [Fact]
    public void Detect_ConfiguredJiraDomain_ReturnsJira()
    {
        var (source, key) = EvidenceLinkSourceDetector.Detect("https://myorg.atlassian.net/browse/ABC-42", "myorg.atlassian.net");

        source.Should().Be(EvidenceLinkSource.Jira);
        key.Should().Be("ABC-42");
    }

    [Fact]
    public void Detect_GenericUrl_ReturnsGeneric()
    {
        var (source, key) = EvidenceLinkSourceDetector.Detect("https://example.com/page", null);

        source.Should().Be(EvidenceLinkSource.Generic);
        key.Should().BeNull();
    }

    [Fact]
    public void Detect_JiraDomainNull_FallsBackToPattern()
    {
        var (source, key) = EvidenceLinkSourceDetector.Detect("https://randomhost.com/browse/KEY-999", null);

        source.Should().Be(EvidenceLinkSource.Jira);
        key.Should().Be("KEY-999");
    }
}
