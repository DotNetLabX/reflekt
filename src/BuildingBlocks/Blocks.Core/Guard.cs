using Blocks.Exceptions;

namespace Blocks.Core;

public static class Guard
{
    public static T NotFound<T>(T? value) where T : class
        => value ?? throw new NotFoundException($"{typeof(T).Name} not found");
}
