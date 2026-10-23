using System.Net;

namespace Blocks.Exceptions;

public class ConflictException : HttpException
{
    public ConflictException(string? message, string? code = null)
        : base(HttpStatusCode.Conflict, message)
    {
        Code = code;
    }
}
