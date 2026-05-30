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
                await WriteError(context, status, ex.Code, ex.Message ?? "Domain error");
            }
            catch (UnauthorizedAccessException)
            {
                await WriteError(context, StatusCodes.Status401Unauthorized,
                    "UNAUTHORIZED", "Authentication required");
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
            "UNPROCESSABLE_ENTITY" => StatusCodes.Status422UnprocessableEntity,
           
            // Специфичные семантические коды — фронт различает по коду в теле ответа,
            // HTTP-статус задан здесь явно.
            "BANNER_NOT_FOUND" => StatusCodes.Status400BadRequest,   // US-CART-04
            "PRODUCT_NOT_FOUND" => StatusCodes.Status404NotFound,    // US-CART-02
            "ALREADY_SUBSCRIBED" => StatusCodes.Status409Conflict,   // US-CART-02

            _ => StatusCodes.Status400BadRequest,
        };

        private static async Task WriteError(
            HttpContext context, int status, string code, string message)
        {
            // Если ответ уже начал писаться — не можем переписать статус, только логируем.
            if (context.Response.HasStarted)
                return;

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";

            var payload = JsonSerializer.Serialize(new { code, message });
            await context.Response.WriteAsync(payload);
        }
    }
}
