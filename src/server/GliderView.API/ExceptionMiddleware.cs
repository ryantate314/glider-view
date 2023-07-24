using GliderView.Service.Exeptions;

namespace GliderView.API
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                switch (ex)
                {
                    case NotFoundException notFoundException:
                    {
                        _logger.LogInformation(ex.Message);

                        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                        await WriteMessage(httpContext.Response, ex.Message);
                        break;
                    }
                    case FlightAlreadyExistsException mappedEx:
                        _logger.LogInformation(ex.Message);

                        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
                        await WriteMessage(httpContext.Response, ex.Message);
                        break;
                    default:
                    {
                        _logger.LogError(ex, ex.Message);

                        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        break;
                    }
                }
            }
        }

        private Task WriteMessage(HttpResponse response, string message)
        {
            return response.WriteAsJsonAsync(new
            {
                message
            });
        }
    }

    public static class MiddlewareExtensions
    {
        public static void UseExceptionMiddleware(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<ExceptionMiddleware>();
        }
    }
}
