// Namespace intentionally differs from project — matches reference project convention.
// LINQ extensions live in Blocks.Linq so they auto-resolve alongside System.Linq usings.
namespace Blocks.Linq;

public static class Extensions
{
    public static TSource SingleOrThrow<TSource>(
        this IEnumerable<TSource> source,
        Func<TSource, bool> predicate)
    {
        var item = source.SingleOrDefault(predicate);
        if (item is null) throw new Blocks.Exceptions.NotFoundException($"{typeof(TSource).Name} not found");
        return item;
    }
}
