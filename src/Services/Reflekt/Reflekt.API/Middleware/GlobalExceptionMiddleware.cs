using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blocks.Domain;
using Blocks.Exceptions;
using FluentValidation;
using FluentValidation.Results;

namespace Reflekt.API.Middleware;

public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger,
    IWebHostEnvironment env)
{
    private const string ServerErrorMessage = "An unexpected error occurred.";
    private const string EndpointValidationMessage = "One or more errors occurred!";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static HttpStatusCode MapStatusCode(Exception ex) => ex switch
    {
        DomainException => HttpStatusCode.Conflict,
        HttpException httpException => httpException.HttpStatusCode,
        ArgumentException => HttpStatusCode.BadRequest,
        _ => HttpStatusCode.InternalServerError
    };

    public static object BuildEndpointValidationResponse(
        List<ValidationFailure> failures, HttpContext context, int statusCode)
        => new ErrorResponse(statusCode, EndpointValidationMessage, context.TraceIdentifier)
        {
            Errors = ToErrorItems(failures)
        };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (OperationCanceledException)
        {
            if (!context.Response.HasStarted)
                context.Response.StatusCode = 499;
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = MapStatusCode(exception);
        var isServerError = statusCode >= HttpStatusCode.InternalServerError;
        if (isServerError)
            logger.LogError(exception, "Unhandled exception. TraceId={TraceId}", context.TraceIdentifier);

        var message = isServerError && !env.IsDevelopment() ? ServerErrorMessage : exception.Message;
        var response = new ErrorResponse((int)statusCode, message, context.TraceIdentifier)
        {
            Code = (exception as HttpException)?.Code,
            Details = env.IsDevelopment() ? exception.StackTrace : null
        };

        return WriteResponseAsync(context, statusCode, response);
    }

    private Task HandleValidationExceptionAsync(HttpContext context, ValidationException exception)
    {
        var response = new ErrorResponse(
            (int)HttpStatusCode.BadRequest,
            "One or more validation errors occurred.",
            context.TraceIdentifier)
        {
            Errors = ToErrorItems(exception.Errors),
            Details = env.IsDevelopment() ? exception.StackTrace : null
        };

        return WriteResponseAsync(context, HttpStatusCode.BadRequest, response);
    }

    private static List<ErrorItem> ToErrorItems(IEnumerable<ValidationFailure> failures)
        => failures.Select(f => new ErrorItem(f.PropertyName, f.ErrorMessage)).ToList();

    private static Task WriteResponseAsync(HttpContext context, HttpStatusCode statusCode, ErrorResponse response)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    public sealed record ErrorResponse(int StatusCode, string Message, string TraceId)
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Code { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<ErrorItem>? Errors { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Details { get; init; }
    }

    public sealed record ErrorItem(string Field, string Message);
}
