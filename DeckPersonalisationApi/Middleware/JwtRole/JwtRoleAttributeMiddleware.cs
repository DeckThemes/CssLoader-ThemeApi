using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto.External.GET;
using DeckPersonalisationApi.Services;
using Microsoft.AspNetCore.Http.Features;

namespace DeckPersonalisationApi.Middleware.JwtRole;

public class JwtRoleAttributeMiddleware
{
    private readonly RequestDelegate _next;

    public JwtRoleAttributeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, JwtService jwt)
    {
        Endpoint? endpoint = context.Features.Get<IEndpointFeature>()?.Endpoint;

        if (endpoint != null)
        {
            JwtRoleReject? reject = endpoint.Metadata.FirstOrDefault(x => x is JwtRoleReject) as JwtRoleReject;
            JwtRoleRequire? require = endpoint.Metadata.FirstOrDefault(x => x is JwtRoleRequire) as JwtRoleRequire;

            if (reject != null || require != null)
            {
                UserJwtDto? user = jwt.DecodeToken(context.Request);

                if (user == null)
                    throw new UnauthorisedException("Failed to decode JWT");
                
                if (reject != null && !(reject.OnlyIfNotPremium && user.HasPermission(Permissions.IsPremium)))
                    user.RejectPermission(reject.Reject);
                
                if (require != null)
                    user.RequirePermission(require.Require);
            }
        }
        
        await _next(context);
    }
}

public static class JwtRoleAttributeMiddlewareExtensions
{
    public static IApplicationBuilder UseJwtRoleAttributes(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JwtRoleAttributeMiddleware>();
    }
}