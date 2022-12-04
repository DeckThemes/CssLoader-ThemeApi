﻿using DeckPersonalisationApi.Extensions;
using DeckPersonalisationApi.Middleware.JwtRole;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto.External.GET;
using DeckPersonalisationApi.Model.Dto.Internal.GET;
using DeckPersonalisationApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeckPersonalisationApi.Controllers;

[ApiController]
[Route("users")]
public class UserController : Controller
{
    private UserService _service;
    private CssThemeService _css;
    private UserService _user;
    private JwtService _jwt;
    private CssSubmissionService _submission;

    public UserController(UserService service, CssThemeService css, UserService user, CssSubmissionService submission, JwtService jwt)
    {
        _service = service;
        _css = css;
        _user = user;
        _submission = submission;
        _jwt = jwt;
    }

    [HttpGet("{id}/css_themes")]
    public IActionResult GetCssThemes(string id, int page = 1, int perPage = 50, string filters = "", string order = "", string search = "")
    {
        User user = _user.GetActiveUserById(id).Require();

        PaginationDto paginationDto = new(page, perPage, filters, order, search);
        PaginatedResponse<CssTheme> response = _css.GetUsersThemes(user, paginationDto);
        return new OkObjectResult(response.ToDto());
    }

    [HttpGet("me/css_themes")]
    [Authorize]
    public IActionResult GetCssThemesMe(int page = 1, int perPage = 50, string filters = "", string order = "", string search = "")
    {
        UserJwtDto user = _jwt.DecodeToken(Request).Require();
        return GetCssThemes(user.Id, page, perPage, filters, order, search);
    }
    
    [HttpGet("{id}/css_themes/filters")]
    [HttpGet("{id}/css_stars/filters")]
    public IActionResult GetCssThemesFilters(string id)
    {
        return new OkObjectResult(new PaginationFilters(_css.Targets, _css.Orders().ToList()));
    }

    [HttpGet("{id}/css_submissions")]
    [Authorize]
    [JwtRoleRequire(Permissions.ViewThemeSubmissions)]
    public IActionResult GetCssSubmissions(string id, int page = 1, int perPage = 50, string filters = "", string order = "", string search = "")
    {
        PaginationDto paginationDto = new(page, perPage, filters, order, search);
        User user = _user.GetActiveUserById(id).Require();
        return new OkObjectResult(_submission.GetSubmissionsFromUser(paginationDto, user).ToDto());
    }

    [HttpGet("me/css_submissions")]
    [Authorize]
    public IActionResult GetCssSubmissionsMe(int page = 1, int perPage = 50, string filters = "", string order = "", string search = "")
    {
        UserJwtDto user = _jwt.DecodeToken(Request).Require();
        return GetCssSubmissions(user.Id, page, perPage, filters, order, search);
    }
    
    [HttpGet("{id}/css_submissions/filters")]
    [Authorize]
    [JwtRoleRequire(Permissions.ViewThemeSubmissions)]
    public IActionResult ViewSubmissionsFilters()
    {
        return new OkObjectResult(new PaginationFilters(_submission.Filters().ToList(), _submission.Orders().ToList()));
    }

    [HttpGet("{id}/css_stars")]
    [Authorize]
    [JwtRoleRequire(Permissions.ManageApi)]
    public IActionResult ViewStarredThemesOfUser(string id, int page = 1, int perPage = 50, string filters = "", string order = "", string search = "")
    {
        User user = _user.GetActiveUserById(id).Require();
        PaginationDto paginationDto = new(page, perPage, filters, order, search);
        PaginatedResponse<CssTheme> response = _css.GetStarredThemesByUser(paginationDto, user);
        return new OkObjectResult(response.ToDto());
    }

    [HttpGet("me/css_stars")]
    [Authorize]
    public IActionResult ViewMyStarredThemes(int page = 1, int perPage = 50, string filters = "", string order = "",
        string search = "")
    {
        UserJwtDto user = _jwt.DecodeToken(Request).Require();
        return ViewStarredThemesOfUser(user.Id, page, perPage, filters, order, search);
    }

    [HttpPost("{id}/css_stars/{themeId}")]
    [Authorize]
    [JwtRoleRequire(Permissions.ManageApi)]
    public IActionResult AddStarToTheme(string id, string themeId)
    {
        User user = _user.GetActiveUserById(id).Require();
        CssTheme theme = _css.GetThemeById(themeId).Require("Theme not found");
        _user.AddStarToTheme(user, theme);
        return new OkResult();
    }
    
    [HttpPost("me/css_stars/{themeId}")]
    [Authorize]
    public IActionResult AddMyStarToTheme(string themeId)
    {
        UserJwtDto user = _jwt.DecodeToken(Request).Require();
        return AddStarToTheme(user.Id, themeId);
    }

    [HttpDelete("{id}/css_stars/{themeId}")]
    [Authorize]
    [JwtRoleRequire(Permissions.ManageApi)]
    public IActionResult RemoveStarFromTheme(string id, string themeId)
    {
        User user = _user.GetActiveUserById(id).Require();
        CssTheme theme = _css.GetThemeById(themeId).Require("Theme not found");
        _user.RemoveStarFromTheme(user, theme);
        return new OkResult();
    }
    
    [HttpDelete("me/css_stars/{themeId}")]
    [Authorize]
    public IActionResult RemoveMyStarFromTheme(string themeId)
    {
        UserJwtDto user = _jwt.DecodeToken(Request).Require();
        return RemoveStarFromTheme(user.Id, themeId);
    }

    [HttpGet("{id}/css_stars/{themeId}")]
    [Authorize]
    [JwtRoleRequire(Permissions.ManageApi)]
    public IActionResult GetStarStatusOfThemeFromUser(string id, string themeId)
    {
        User user = _user.GetUserById(id).Require("User not found");
        CssTheme theme = _css.GetThemeById(themeId).Require("Theme not found");
        return new OkObjectResult(new HasThemeStarredDto(_user.HasThemeStarred(user, theme)));
    }
    
    [HttpGet("me/css_stars/{themeId}")]
    [Authorize]
    public IActionResult GetStarStatusOfThemeFromMe(string themeId)
    {
        UserJwtDto user = _jwt.DecodeToken(Request).Require();
        return GetStarStatusOfThemeFromUser(user.Id, themeId);
    }
}
