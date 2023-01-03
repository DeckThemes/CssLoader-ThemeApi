#region

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Extensions;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto;
using DeckPersonalisationApi.Model.Dto.External.GET;
using Microsoft.IdentityModel.Tokens;

#endregion

namespace DeckPersonalisationApi.Services;

public class JwtService
{
    private AppConfiguration _config;

    public JwtService(AppConfiguration config)
    {
        _config = config;
    }

    public string CreateToken(UserJwtDto user)
    {
        string issuer = _config.JwtIssuer;
        string audience = _config.JwtAudience;
        byte[] key = Encoding.ASCII.GetBytes(_config.JwtKey);
        SecurityTokenDescriptor descriptor = new()
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("Id", user.Id),
                new Claim("Name", user.Username),
                new Claim("Permissions", ((int)user.Permissions).ToString()),
                new Claim("Avatar", user.Avatar),
                new Claim("Validation", user.ValidationToken)
            }),
            Expires = DateTime.UtcNow.AddMinutes(10),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512Signature)
        };

        JwtSecurityTokenHandler handler = new();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    public string RenewToken(string key, User user)
    {
        JwtSecurityTokenHandler handler = new();
        JwtSecurityToken? token = handler.ReadToken(key) as JwtSecurityToken;

        if (token == null)
            throw new BadRequestException("Not a JWT token");

        if (token.ValidTo.AddDays(7) < DateTime.Now)
            throw new BadRequestException("Token is too old to be refreshed");

        UserJwtDto dto = DecodeToken(key).Require("JWT is invalid");

        if (dto.Id != user.Id)
            throw new BadRequestException("This seems to be someone else's JWT");

        if ((dto.Permissions.HasPermission(Permissions.FromApiToken) ? dto.ValidationToken != user.ApiToken : dto.ValidationToken != user.ValidationToken))
            throw new BadRequestException("Validation failed on token. Please re-login");
        
        UserJwtDto refreshedToken = new(user);

        if (dto.Permissions.HasPermission(Permissions.FromApiToken))
        {
            refreshedToken.ValidationToken = user.ApiToken!;
            refreshedToken.Permissions |= Permissions.FromApiToken;
        }
        
        return CreateToken(refreshedToken);
    }

    public UserJwtDto? DecodeToken(HttpRequest request)
    {
        string? auth = request.Headers.Authorization;

        if (string.IsNullOrEmpty(auth) || !auth.StartsWith("Bearer "))
            return null;

        string key = auth[7..];
        return DecodeToken(key);
    }
    
    public UserJwtDto? DecodeToken(string key)
    {
        JwtSecurityTokenHandler handler = new();
        JwtSecurityToken? token = handler.ReadToken(key) as JwtSecurityToken;

        if (token == null)
            return null;

        string? id = token.Claims.FirstOrDefault(x => x.Type == "Id")?.Value;
        string? name = token.Claims.FirstOrDefault(x => x.Type == "Name")?.Value;
        string? permissions = token.Claims.FirstOrDefault(x => x.Type == "Permissions")?.Value;
        string? avatar = token.Claims.FirstOrDefault(x => x.Type == "Avatar")?.Value;
        string? validation = token.Claims.FirstOrDefault(x => x.Type == "Validation")?.Value;

        if (id == null || name == null || permissions == null || avatar == null || validation == null)
            return null;
        
        return new(id, name, avatar, int.Parse(permissions), validation);
    }

    public bool ValidateToken(string token, bool ignoreTime)
    {
        string issuer = _config.JwtIssuer;
        string audience = _config.JwtAudience;
        byte[] key = Encoding.ASCII.GetBytes(_config.JwtKey);
        JwtSecurityTokenHandler handler = new();
        try
        {
            handler.ValidateToken(token, new TokenValidationParameters()
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = _config.JwtValidateIssuer,
                ValidateAudience = _config.JwtValidateAudience,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = !ignoreTime
            }, out SecurityToken validatedToken);
        }
        catch
        {
            return false;
        }

        return true;
    }
}