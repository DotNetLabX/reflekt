namespace Reflekt.API.Auth;

internal static class AllowlistChecker
{
    public static bool IsAllowed(string? email, IReadOnlyCollection<string> allowed)
        => !string.IsNullOrWhiteSpace(email)
           && allowed.Contains(email, StringComparer.OrdinalIgnoreCase);
}
