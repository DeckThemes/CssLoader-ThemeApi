using DeckPersonalisationApi.Model;

namespace DeckPersonalisationApi.Middleware.CacheControl;

public class CacheControlMiddleware
{
    private readonly RequestDelegate _next;

    public CacheControlMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(state => {
            var httpContext = (HttpContext)state;

            try
            {
                if (httpContext.Response.ContentType == BlobType.Jpg.GetContentType() ||
                    httpContext.Response.ContentType == BlobType.Png.GetContentType())
                {
                    httpContext.Response.Headers.CacheControl = "public, max-age=86400";
                }
                else
                {
                    httpContext.Response.Headers.CacheControl = "no-store";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[FAIL] Failed to write CacheControl header: {e.Message}");
            }

            return Task.CompletedTask;
        }, context);
        
        await _next(context);
    }
}

public static class CacheControlMiddlewareExtensions
{
    public static IApplicationBuilder UseCacheControlMiddleware(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CacheControlMiddleware>();
    }
}