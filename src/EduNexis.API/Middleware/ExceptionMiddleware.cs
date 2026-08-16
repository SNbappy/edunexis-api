using EduNexis.Domain.Common;
using EduNexis.Domain.Exceptions;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace EduNexis.API.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message, errors) = exception switch
        {
            ValidationException vex => (
                HttpStatusCode.BadRequest,
                "Validation failed.",
                vex.Errors.Select(e => e.ErrorMessage).ToList()),

            NotFoundException nfe => (
                HttpStatusCode.NotFound,
                nfe.Message,
                (List<string>?)null),

            UnauthorizedException ue => (
                HttpStatusCode.Forbidden,
                ue.Message,
                (List<string>?)null),

            ProfileIncompleteException pie => (
                HttpStatusCode.BadRequest,
                pie.Message,
                (List<string>?)null),

            QuotaExceededException qee => (
                HttpStatusCode.Forbidden,
                qee.Message,
                (List<string>?)null),

            AccessExpiredException aee => (
                HttpStatusCode.Forbidden,
                aee.Message,
                (List<string>?)null),

            // 409, not 403: the caller is allowed to do this in general — the
            // course is simply in a state that refuses writes, and unarchiving
            // resolves it. That is a conflict, not a permission problem.
            CourseArchivedException cae => (
                HttpStatusCode.Conflict,
                cae.Message,
                (List<string>?)null),

            DomainException de => (
                HttpStatusCode.BadRequest,
                de.Message,
                (List<string>?)null),

            _ => (
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred.",
                (List<string>?)null)
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse.Fail(message, errors);
        var json = JsonSerializer.Serialize(response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        await context.Response.WriteAsync(json);
    }
}
