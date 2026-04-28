using System.Net;
using System.Text.Json;
using Api.DTOs;
using Domain.Exceptions;

namespace Api.Extensions
{
    /// <summary>
    /// Централизованная обработка исключений.
    /// Маппит доменные исключения → HTTP статусы согласно OpenAPI контракту.
    /// </summary>
    public sealed class ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        public async Task InvokeAsync(HttpContext ctx)
        {
            try
            {
                await next(ctx);
            }
            catch (Exception ex)
            {
                await HandleAsync(ctx, ex);
            }
        }

        private async Task HandleAsync(HttpContext ctx, Exception ex)
        {
            var (status, message) = ex switch
            {
                NotFoundException e => (HttpStatusCode.NotFound, e.Message),
                ValidationException e => (HttpStatusCode.BadRequest, e.Message),
                MissingParameterException e => (HttpStatusCode.BadRequest, e.Message),
                AmbiguousParameterException e => (HttpStatusCode.BadRequest, e.Message),
                OrphanNodeException e => (HttpStatusCode.UnprocessableEntity, e.Message),
                UpstreamUnavailableException e => (HttpStatusCode.ServiceUnavailable, e.Message),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
            };

            if (status == HttpStatusCode.InternalServerError)
                logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                    ctx.Request.Method, ctx.Request.Path);
            else
                logger.LogWarning("Domain exception [{Status}]: {Message}",
                    (int)status, message);

            ctx.Response.StatusCode = (int)status;
            ctx.Response.ContentType = "application/json";

            await ctx.Response.WriteAsync(
                JsonSerializer.Serialize(new ErrorResponse(message), JsonOpts));
        }
    }
}
