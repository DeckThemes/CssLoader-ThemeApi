#region

using System.ComponentModel;
using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Extensions;
using DeckPersonalisationApi.Middleware.JwtRole;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto;
using DeckPersonalisationApi.Model.Dto.External.GET;
using DeckPersonalisationApi.Model.Dto.External.POST;
using DeckPersonalisationApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

#endregion

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
        return new UriDto(_user.BuildDiscordOauthUri(redirect)).Ok();
    }

    [HttpPost("authenticate_discord")]
    public IActionResult GetToken(DiscordAuthenticatePostDto auth)
    {
        return new TokenGetDto(_user.GenerateTokenViaDiscord(auth.Code, auth.RedirectUrl)).Ok();
    }

    [HttpPost("authenticate_token")]
    public IActionResult GetTokenViaApiToken(ApiTokenPostDto auth)
    {
        return new TokenGetDto(_user.GenerateTokenViaApiToken(auth.Token)).Ok();
    }

    [HttpPost("refresh_token")]
    // Uses manual auth checks
    public IActionResult RefreshJwtToken()
    {
        string? auth = Request.Headers.Authorization;

        if (string.IsNullOrEmpty(auth) || !auth.StartsWith("Bearer "))
            throw new BadRequestException("No auth token set");

        string key = auth[7..];

        if (!_jwt.ValidateToken(key, true))
            throw new BadRequestException("Cannot validate token");

        UserJwtDto token = _jwt.DecodeToken(key).Require("Cannot extract token");
        User user = _user.GetActiveUserById(token.Id).Require("Cannot find user");
        return new TokenGetDto(_jwt.RenewToken(key, user)).Ok();
    }

    [HttpGet("me_full")]
    [Description("Contacts the database to give all stored userdata")]
    [Authorize]
    public IActionResult GetFullUser()
    {
        UserJwtDto token = _jwt.DecodeToken(Request).Require();
        User user = _user.GetUserById(token.Id).Require();
        return user.Ok();
    }
    
    [HttpGet("me")]
    [Description("Reads out the current authenticated user token")]
    [Authorize]
    public IActionResult GetUser()
    {
        return _jwt.DecodeToken(Request).Require().Ok();
    }
}