namespace Reflekt.Persistence;

internal static class DbInitializer
{
    public static Task SeedAsync(ReflektDbContext db, CancellationToken ct = default)
        => Task.CompletedTask; // Features populate this as their aggregates land.
}
