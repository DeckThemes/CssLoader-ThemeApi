namespace DeckPersonalisationApi.Middleware.CookieConverter;

public class AuthTokenCookieToAuthHeaderMiddleware
{
    private readonly RequestDelegate _next;

    public AuthTokenCookieToAuthHeaderMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string? cookie = context.Request.Cookies["authToken"];

        if (!string.IsNullOrWhiteSpace(cookie) && !context.Request.Headers.ContainsKey("Authorization"))
            context.Request.Headers.Authorization = $"Bearer {cookie}";
        
        await _next(context);
    }
}

public static class AuthTokenCookieToAuthHeaderMiddlewareExtensions
{
    public static IApplicationBuilder UseAuthTokenCookieToAuthHeader(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthTokenCookieToAuthHeaderMiddleware>();
    }
}