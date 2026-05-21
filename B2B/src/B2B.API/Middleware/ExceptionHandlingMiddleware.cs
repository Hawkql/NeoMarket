using System.Text.Json;
using B2B.Domain.Common;
using FluentValidation;
namespace B2B.Api.Middleware
{
    public sealed class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (ValidationException ex)
            {
                // Первое сообщение об ошибке — для простоты (можно вернуть все)
                var message = ex.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed";
                await WriteError(context, StatusCodes.Status400BadRequest,
                    "INVALID_REQUEST", message);
            }
            catch (DomainException ex)
            {
                var status = MapDomainCodeToStatus(ex.Code);
                await WriteError(context, status, ex.Code, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");
                await WriteError(context, StatusCodes.Status500InternalServerError,
                    "INTERNAL_ERROR", "An unexpected error occurred");
            }
        }

        private static int MapDomainCodeToStatus(string code) => code switch
        {
            "INVALID_REQUEST" => StatusCodes.Status400BadRequest,
            "NOT_FOUND" => StatusCodes.Status404NotFound,
            "NOT_OWNER" => StatusCodes.Status403Forbidden,
            "FORBIDDEN" => StatusCodes.Status403Forbidden,
            "CONFLICT" => StatusCodes.Status409Conflict,
            "INSUFFICIENT_STOCK" => StatusCodes.Status409Conflict,
            "INVALID_STATE_TRANSITION" => StatusCodes.Status400BadRequest,
            "CYCLE_DETECTED" => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };

        private static async Task WriteError(
            HttpContext context, int status, string code, string message)
        {
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";

            var payload = JsonSerializer.Serialize(new { code, message });
            await context.Response.WriteAsync(payload);
        }
    }
}
