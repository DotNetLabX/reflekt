namespace Reflekt.API.Auth;

public sealed class AuthSettings
{
    public GoogleCredentials Google { get; init; } = new();
    public string? DevFrontendOrigin { get; init; }

    public sealed class GoogleCredentials
    {
        public string ClientId { get; init; } = "";
        public string ClientSecret { get; init; } = "";
    }
}
