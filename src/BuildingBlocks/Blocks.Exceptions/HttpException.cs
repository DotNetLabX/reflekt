using System.Net;

namespace Blocks.Exceptions;

public class HttpException : Exception
{
    public HttpException(HttpStatusCode statusCode, string? message, Exception ex)
        : base(message, ex)
    {
        HttpStatusCode = statusCode;
    }

    public HttpException(HttpStatusCode statusCode, string? message)
        : base(string.IsNullOrEmpty(message) ? statusCode.ToString() : message)
    {
        HttpStatusCode = statusCode;
    }

    public HttpStatusCode HttpStatusCode { get; }
    public int StatusCode => (int)HttpStatusCode;
    public string? Code { get; init; }
}
