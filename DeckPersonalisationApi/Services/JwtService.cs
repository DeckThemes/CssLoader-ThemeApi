using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DeckPersonalisationApi.Services;

public class JwtService
{
    private IConfiguration _config;

    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    public string CreateToken(DiscordUserJwtDto user)
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
                new Claim("Permissions", ((int)user.Permissions).ToString())
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

    public DiscordUserJwtDto? DecodeToken(HttpRequest request)
    {
        string? auth = request.Headers.Authorization;

        if (string.IsNullOrEmpty(auth) || !auth.StartsWith("Bearer "))
            return null;

        string key = auth[7..];
        return DecodeToken(key);
    }
    
    public DiscordUserJwtDto? DecodeToken(string key)
    {
        JwtSecurityTokenHandler handler = new();
        JwtSecurityToken? token = handler.ReadToken(key) as JwtSecurityToken;

        if (token == null)
            return null;

        string? id = token.Claims.FirstOrDefault(x => x.Type == "Id")?.Value;
        string? name = token.Claims.FirstOrDefault(x => x.Type == "Name")?.Value;
        string? permissions = token.Claims.FirstOrDefault(x => x.Type == "Permissions")?.Value;

        if (id == null || name == null || permissions == null)
            return null;
        
        return new(id, name, int.Parse(permissions));
    }
}