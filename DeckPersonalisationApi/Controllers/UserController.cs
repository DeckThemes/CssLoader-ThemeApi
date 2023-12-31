using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Extensions;
using DeckPersonalisationApi.Middleware.JwtRole;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto.External.GET;
using DeckPersonalisationApi.Model.Dto.External.PATCH;
using DeckPersonalisationApi.Model.Dto.Internal.GET;
using DeckPersonalisationApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeckPersonalisationApi.Controllers;

[ApiController]
[Route("users")]
public class UserController : Controller
{
    private ThemeService _theme;
    private UserService _user;
    private JwtService _jwt;
    private SubmissionService _submission;

    public UserController(ThemeService theme, UserService user, SubmissionService submission, JwtService jwt)
    {
        _theme = theme;
        _user = user;
        _submission = submission;
        _jwt = jwt;
    }

    [HttpGet("{id}/themes")]
    public IActionResult GetCssThemes(string id, int page = 1, int perPage = 50, string filters = "", string order = "", string search = "")
    {
        User user = _user.GetActiveUserById(id).Require();

        PaginationDto paginationDto = new(page, perPage, filters, order, search);
        PaginatedResponse<CssTheme> response = _theme.GetUsersThemes(user, paginationDto);
        return response.Ok();
    }

    [HttpGet("me/themes")]
    [Authorize]
    public IActionResult GetCssThemesMe(int page = 1, int perPage = 50, string filters = "", string order = "", string search = "")
    {
        UserJwtDto user = _jwt.DecodeToken(Request).Require();
        return GetCssThemes(user.Id, page, perPage, filters, order, search);
    }
    
    [HttpGet("me/themes/private")]
    [Authorize]
    public IActionResult GetCssThemesMePrivate(int page = 1, int perPage = 50, string filters = "", string order = "", string search = "")
    {
        UserJwtDto userJwt = _jwt.DecodeToken(Request).Require();
        User user = _user.GetActiveUserById(userJwt.Id).Require();
        PaginationDto paginationDto = new(page, perPage, filters, order, search);
        PaginatedResponse<CssTheme> response = _theme.GetUsersThemes(user, paginationDto, PostVisibility.Private);
        return response.Ok();
    }

    [HttpPost("{id}/logout_all")]
    [Authorize]
    [JwtRoleRequire(Permissions.ManageApi)]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult RenewValidationToken(string id)
    {
        _user.ResetValidationToken(id);
        return new OkResult();
    }
    
    [HttpPost("me/logout_all")]
    [Authorize]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult RenewMyValidationToken()
    {
        UserJwtDto user = _jwt.DecodeToken(Request).Require();
        return RenewValidationToken(user.Id);
    }
    
    [HttpGet("{id}/themes/filters")]
    public IActionResult GetCssThemesFilters(string id, string type = "")
    {
        if (id == "me")
            id = _jwt.DecodeToken(Request).Require().Id;
        
        User user = _user.GetActiveUserById(id).Require();

        return new PaginationFilters(_theme.FiltersWithCount(type, user), _theme.Orders().ToList()).Ok();
    }
    
    [HttpGet("{id}/stars/filters")]
    public IActionResult GetCssThemesFiltersStars(string id, string type = "")
    {
        if (id == "me")
            id = _jwt.DecodeToken(Request).Require().Id;
        
        User user = _user.GetActiveUserById(id).Require();
        ThemeType? themeType = null;

        return new PaginationFilters(_theme.FiltersWithCount(type, user, true), _theme.Orders().ToList()).Ok();
    }

    [HttpGet("me/stars/filters")]
    [Authorize]
    public IActionResult GetCssThemesFiltersStarsMe(string type = "")
        => GetCssThemesFiltersStars("me", type);

    [HttpGet("{id}/submissions")]
    [Authorize]
    [JwtRoleRequire(Permissions.ViewThemeSubmissions)]
    public IActionResult GetCssSubmissions(string id, int page = 1, int perPage = 50, string filters = "", string order = "", string search = "")
    {
        PaginationDto paginationDto = new(page, perPage, filters, order, search);
        User user = _user.GetActiveUserById(id).Require();
        return _submission.GetSubmissionsFromUser(paginationDto, user).Ok();
    }

    [HttpGet("me/submissions")]
    [Authorize]
    public IActionResult GetCssSubmissionsMe(int page = 1, int perPage = 50, string filters = "", string order = "", string search = "")
    {
        UserJwtDto user = _jwt.DecodeToken(Request).Require();
        return GetCssSubmissions(user.Id, page, perPage, filters, order, search);
    }
    
    [HttpGet("{id}/submissions/filters")]
    [Authorize]
    [JwtRoleRequire(Permissions.ManageApi)]
    public IActionResult ViewSubmissionsFilters(string id, string type = "")
    {
        if (id == "me")
            id = _jwt.DecodeToken(Request).Require().Id;
        
        User user = _user.GetActiveUserById(id).Require();
        ThemeType? themeType = null;
        
        if (type.ToLower() == "css")
            themeType = ThemeType.Css;
        else if (type.ToLower() == "audio")
            themeType = ThemeType.Audio;
        
        return new PaginationFilters(_submission.FiltersWithCount(themeType, user), _submission.Orders().ToList()).Ok();
    }

    [HttpGet("me/submissions/filters")]
    [Authorize]
    public IActionResult ViewSubmissionFiltersMe(string target = "")
        => ViewSubmissionsFilters("me", target);

    [HttpGet("{id}/stars")]
    public IActionResult ViewStarredThemesOfUser(string id, int page = 1, int perPage = 50, string filters = "", string order = "", string search = "")
    {
        User user = _user.GetActiveUserById(id).Require();
        PaginationDto paginationDto = new(page, perPage, filters, order, search);
        PaginatedResponse<CssTheme> response = _theme.GetStarredThemesByUser(paginationDto, user);
        return response.Ok();
    }

    [HttpGet("me/stars")]
    [Authorize]
    public IActionResult ViewMyStarredThemes(int page = 1, int perPage = 50, string filters = "", string order = "",
        string search = "")
    {
        UserJwtDto user = _jwt.DecodeToken(Request).Require();
        return ViewStarredThemesOfUser(user.Id, page, perPage, filters, order, search);
    }

    [HttpPost("{id}/stars/{themeId}")]
    [Authorize]
    [JwtRoleRequire(Permissions.ManageApi)]
    public IActionResult AddStarToTheme(string id, string themeId)
    {
        User user = _user.GetActiveUserById(id).Require();
        CssTheme theme = _theme.GetThemeById(themeId).Require("Theme not found");
        _user.AddStarToTheme(user, theme);
        return new OkResult();
    }
    
    [HttpPost("me/stars/{themeId}")]
    [Authorize]
    public IActionResult AddMyStarToTheme(string themeId)
    {
        UserJwtDto user = _jwt.DecodeToken(Request).Require();
        return AddStarToTheme(user.Id, themeId);
    }

    [HttpDelete("{id}/stars/{themeId}")]
    [Authorize]
    [JwtRoleRequire(Permissions.ManageApi)]
    public IActionResult RemoveStarFromTheme(string id, string themeId)
    {
        User user = _user.GetActiveUserById(id).Require();
        CssTheme theme = _theme.GetThemeById(themeId).Require("Theme not found");
        _user.RemoveStarFromTheme(user, theme);
        return new OkResult();
    }
    
    [HttpDelete("me/stars/{themeId}")]
    [Authorize]
    public IActionResult RemoveMyStarFromTheme(string themeId)
    {
        UserJwtDto user = _jwt.DecodeToken(Request).Require();
        return RemoveStarFromTheme(user.Id, themeId);
    }

    [HttpGet("{id}/stars/{themeId}")]
    [Authorize]
    [JwtRoleRequire(Permissions.ManageApi)]
    public IActionResult GetStarStatusOfThemeFromUser(string id, string themeId)
    {
        User user = _user.GetActiveUserById(id).Require("User not found");
        CssTheme theme = _theme.GetThemeById(themeId).Require("Theme not found");
        return new HasThemeStarredDto(_user.HasThemeStarred(user, theme)).Ok();
    }
    
    [HttpGet("me/stars/{themeId}")]
    [Authorize]
    public IActionResult GetStarStatusOfThemeFromMe(string themeId)
    {
        UserJwtDto user = _jwt.DecodeToken(Request).Require();
        return GetStarStatusOfThemeFromUser(user.Id, themeId);
    }

    [HttpGet("{id}")]
    public IActionResult GetUser(string id)
        => _user.GetUserById(id).Require().Ok();

    [HttpPatch("{id}")]
    [Authorize]
    [JwtRoleRequire(Permissions.ManageApi)]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult EditUser(string id, UserPatchDto patch)
    {
        User user = _user.GetUserById(id).Require();
        _user.SetUserPreferences(user, patch.Email, patch.Active, PermissionExt.FromList(patch.Permissions));
        return new OkResult();
    }

    [HttpPatch("me")]
    [Authorize]
    [JwtRoleReject((Permissions.FromApiToken))]
    public IActionResult EditUserPreferences(UserPatchDto patch)
    {
        UserJwtDto dto = _jwt.DecodeToken(Request).Require();

        if (patch.EditsAdminFields())
            throw new UnauthorisedException("Editing Admin fields while not being an admin yourself is disallowed");

        return EditUser(dto.Id, patch);
    }
    
    [HttpGet("{id}/token")]
    [Authorize]
    [JwtRoleReject(Permissions.FromApiToken)]
    [JwtRoleRequire(Permissions.ManageApi)]
    public IActionResult GetApiToken(string id)
    {
        string token = _user.GetApiToken(id).Require();
        return new TokenGetDto(token).Ok();
    }
    
    [HttpGet("me/token")]
    [Authorize]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult GetApiToken()
    {
        UserJwtDto dto = _jwt.DecodeToken(Request)!;
        return GetApiToken(dto.Id);
    }
}
