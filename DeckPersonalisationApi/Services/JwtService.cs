#region

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto;
using DeckPersonalisationApi.Model.Dto.External.GET;
using Microsoft.IdentityModel.Tokens;

#endregion

namespace DeckPersonalisationApi.Services;

public class JwtService
{
    private IConfiguration _config;

    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    public string CreateToken(UserJwtDto user)
    {
        string issuer = _config["Jwt:Issuer"]!;
        string audience = _config["Jwt:Audience"]!;
        byte[] key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]!);
        SecurityTokenDescriptor descriptor = new()
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("Id", user.Id),
                new Claim("Name", user.Username),
                new Claim("Permissions", ((int)user.Permissions).ToString()),
                new Claim("Avatar", user.Avatar),
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512Signature)
        };

        JwtSecurityTokenHandler handler = new();
        return handler.WriteToken(handler.CreateToken(descriptor));
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

        if (id == null || name == null || permissions == null || avatar == null)
            return null;
        
        return new(id, name, avatar, int.Parse(permissions));
    }
}