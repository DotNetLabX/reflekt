using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blocks.Domain;
using Blocks.Exceptions;
using FluentValidation;

namespace Reflekt.API.Middleware;

public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger,
    IWebHostEnvironment env)
{
    private const string ServerErrorMessage = "An unexpected error occurred.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static HttpStatusCode MapStatusCode(Exception ex) => ex switch
    {
        DomainException => HttpStatusCode.Conflict,
        HttpException httpException => httpException.HttpStatusCode,
        ArgumentException => HttpStatusCode.BadRequest,
        _ => HttpStatusCode.InternalServerError
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
            Errors = exception.Errors.Select(e => new ValidationError(e.PropertyName, e.ErrorMessage)).ToList(),
            Details = env.IsDevelopment() ? exception.StackTrace : null
        };

        return WriteResponseAsync(context, HttpStatusCode.BadRequest, response);
    }

    private static Task WriteResponseAsync(HttpContext context, HttpStatusCode statusCode, ErrorResponse response)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private sealed record ErrorResponse(int StatusCode, string Message, string TraceId)
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyList<ValidationError>? Errors { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Details { get; init; }
    }

    private sealed record ValidationError(string PropertyName, string ErrorMessage);
}
