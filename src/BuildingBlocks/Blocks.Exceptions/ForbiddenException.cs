using System.Net;

namespace Blocks.Exceptions;

public class ForbiddenException : HttpException
{
    public ForbiddenException(string? message)
        : base(HttpStatusCode.Forbidden, message) { }
}
