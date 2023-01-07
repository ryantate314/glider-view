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
                        await httpContext.Response.WriteAsync(ex.Message);
                        break;
                    }
                    default:
                    {
                        _logger.LogError(ex, ex.Message);

                        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        break;
                    }
                }
            }
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
