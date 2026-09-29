using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using FluentValidation; 
using System.Threading;
using System.Threading.Tasks;

namespace OmniMart.API.Middlewares
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var traceId = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
            _logger.LogError(exception, "System Error [{TraceId}]: {Message}", traceId, exception.Message);

            if (exception is ValidationException validationException)
            {
                httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                var validationResponse = new
                {
                    IsSuccess = false,
                    TraceId = traceId, 
                    ValidationErrors = validationException.Errors.Select(e => e.ErrorMessage).ToList(),
                    ErrorType = 422
                };

                await httpContext.Response.WriteAsJsonAsync(validationResponse, cancellationToken);
                return true;
            }

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            var errorResponse = new
            {
                IsSuccess = false,
                Message = "An unexpected system error occurred. Please contact technical support and provide them with the tracking number.",
                TraceId = traceId 
            };

            await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);
            return true;
        }
    }
}
