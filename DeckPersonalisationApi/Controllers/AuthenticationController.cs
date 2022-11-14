using System.ComponentModel;
using DeckPersonalisationApi.Extensions;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto;
using DeckPersonalisationApi.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeckPersonalisationApi.Controllers;

[ApiController]
[Route("auth")]
public class AuthenticationController : Controller
{
    private UserService _user;
    private JwtService _jwt;
    
    public AuthenticationController(UserService user, JwtService jwt)
    {
        _user = user;
        _jwt = jwt;
    }

    [HttpGet("oauth_redirect")]
    public IActionResult GetDiscordUrl(string redirect = "https://localhost/")
    {
        return new OkObjectResult(new DiscordUrlDto(_user.BuildDiscordOauthUri(redirect)));
    }

    [HttpPost("get_token")]
    public IActionResult GetToken(DiscordAuthenticateDto authenticate)
    {
        return new OkObjectResult(new TokenDto(_user.GenerateTokenViaDiscord(authenticate.Code, authenticate.RedirectUrl)));
    }

    [HttpGet("me_full")]
    [Description("Contacts the database to give all stored userdata")]
    [Authorize]
    public IActionResult GetFullUser()
    {
        DiscordUserJwtDto? token = _jwt.DecodeToken(Request);

        if (token == null)
            return new NotFoundResult();

        User? user = _user.GetUserById(token.Id);

        if (user == null)
            return new NotFoundResult();

        return new ObjectResult(new UserDto(user));
    }
    
    [HttpGet("me")]
    [Description("Reads out the current authenticated user token")]
    [Authorize]
    public IActionResult GetUser()
    {
        DiscordUserJwtDto? token = _jwt.DecodeToken(Request);

        if (token == null)
            return new NotFoundResult();

        return new OkObjectResult(token);
    }
}