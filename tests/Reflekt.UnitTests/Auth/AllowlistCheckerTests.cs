using FluentAssertions;
using Reflekt.API.Auth;

namespace Reflekt.UnitTests.Auth;

public class AllowlistCheckerTests
{
    [Fact]
    public void EmptyAllowlist_ReturnsFalse()
    {
        AllowlistChecker.IsAllowed("user@example.com", []).Should().BeFalse();
    }

    [Fact]
    public void ExactMatch_ReturnsTrue()
    {
        AllowlistChecker.IsAllowed("user@example.com", ["user@example.com"]).Should().BeTrue();
    }

    [Fact]
    public void CaseInsensitiveMatch_ReturnsTrue()
    {
        AllowlistChecker.IsAllowed("Foo@Bar.com", ["foo@bar.com"]).Should().BeTrue();
    }

    [Fact]
    public void NullEmail_ReturnsFalse()
    {
        AllowlistChecker.IsAllowed(null, ["user@example.com"]).Should().BeFalse();
    }

    [Fact]
    public void WhitespaceEmail_ReturnsFalse()
    {
        AllowlistChecker.IsAllowed("   ", ["user@example.com"]).Should().BeFalse();
    }

    [Fact]
    public void UnlistedEmail_ReturnsFalse()
    {
        AllowlistChecker.IsAllowed("other@example.com", ["user@example.com"]).Should().BeFalse();
    }
}
