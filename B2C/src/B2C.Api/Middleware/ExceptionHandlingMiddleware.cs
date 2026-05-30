using System.Text.Json;
using B2C.Domain.Common;
using FluentValidation;

namespace B2C.Api.Middleware
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
                var message = ex.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed";
                await WriteError(context, StatusCodes.Status400BadRequest, "INVALID_REQUEST", message);
            }
            catch (DomainException ex)
            {
                var status = MapDomainCodeToStatus(ex.Code);
                await WriteError(context, status, ex.Code, ex.Message ?? "Domain error", ex.Details);
            }
            catch (UnauthorizedAccessException)
            {
                await WriteError(context, StatusCodes.Status401Unauthorized,
                    "UNAUTHORIZED", "Authentication required");
            }
            catch (HttpRequestException ex)
            {
                // Сетевой fail к B2B / любому HTTP-зависимому ресурсу — Service Unavailable.
                // Семантически точнее 500: это не наша ошибка, а недоступность зависимости.
                _logger.LogWarning(ex, "Upstream HTTP dependency unavailable");
                await WriteError(context, StatusCodes.Status503ServiceUnavailable,
                    "B2B_UNAVAILABLE", "Upstream service is temporarily unavailable");
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
            // Базовые коды.
            "INVALID_REQUEST" => StatusCodes.Status400BadRequest,
            "NOT_FOUND" => StatusCodes.Status404NotFound,
            "FORBIDDEN" => StatusCodes.Status403Forbidden,
            "CONFLICT" => StatusCodes.Status409Conflict,
            "UNAUTHORIZED" => StatusCodes.Status401Unauthorized,

            // Семантические коды (фронт различает по code в теле ответа).
            "BANNER_NOT_FOUND" => StatusCodes.Status400BadRequest,    // US-CART-04
            "PRODUCT_NOT_FOUND" => StatusCodes.Status404NotFound,     // US-CART-02
            "ALREADY_SUBSCRIBED" => StatusCodes.Status409Conflict,    // US-CART-02
            "UNPROCESSABLE_ENTITY" => StatusCodes.Status422UnprocessableEntity,  // US-CAT-05
            "RESERVE_FAILED" => StatusCodes.Status409Conflict,        // US-ORD-01
            "CANCEL_NOT_ALLOWED" => StatusCodes.Status409Conflict,    // US-ORD-03
            "B2B_UNAVAILABLE" => StatusCodes.Status503ServiceUnavailable,  // US-ORD-01, CAT-01

            _ => StatusCodes.Status400BadRequest,
        };

        private static async Task WriteError(
            HttpContext context, int status, string code, string message, object? details = null)
        {
            if (context.Response.HasStarted)
                return;

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";

            // Формат: { code, message, details? }
            // details — опциональный объект (failed_items / current_status / ...).
            object payload = details is null
                ? new { code, message }
                : new { code, message, details };

            // PropertyNamingPolicy = SnakeCaseLower (как в Program.cs) — поля в snake_case.
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            });
            await context.Response.WriteAsync(json);
        }
    }
}