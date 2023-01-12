using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Extensions;
using DeckPersonalisationApi.Middleware.JwtRole;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto.External.GET;
using DeckPersonalisationApi.Model.Dto.External.POST;
using DeckPersonalisationApi.Model.Dto.Internal.GET;
using DeckPersonalisationApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeckPersonalisationApi.Controllers;

[ApiController]
[Route("themes")]
public class ThemeController : Controller
{
    private JwtService _jwt;
    private UserService _user;
    private ThemeService _theme;

    public ThemeController(JwtService jwt, ThemeService theme, UserService user)
    {
        _jwt = jwt;
        _theme = theme;
        _user = user;
    }

    [HttpGet]
    public IActionResult GetThemes(int page = 1, int perPage = 50, string filters = "", string order = "", string search = "")
    {
        PaginationDto paginationDto = new(page, perPage, filters, order, search);
        PaginatedResponse<CssTheme> response = _theme.GetApprovedThemes(paginationDto);
        return response.Ok();
    }
    
    [HttpGet("filters")]
    public IActionResult GetThemesFilters(string type = "")
    {
        ThemeType? themeType = null;

        if (type.ToLower() == "css")
            themeType = ThemeType.Css;
        else if (type.ToLower() == "audio")
            themeType = ThemeType.Audio;

        return new PaginationFilters(_theme.FiltersWithCount(themeType, null), _theme.Orders().ToList()).Ok();
    }
    
    [HttpGet("awaiting_approval/filters")]
    [Authorize]
    [JwtRoleRequire(Permissions.ViewThemeSubmissions)]
    public IActionResult GetUnapprovedThemesFilters(string type = "")
    {
        ThemeType? themeType = null;

        if (type.ToLower() == "css")
            themeType = ThemeType.Css;
        else if (type.ToLower() == "audio")
            themeType = ThemeType.Audio;

        return new PaginationFilters(_theme.FiltersWithCount(themeType, null, approved: false), _theme.Orders().ToList()).Ok();
    }

    [HttpGet("awaiting_approval")]
    [Authorize]
    [JwtRoleRequire(Permissions.ViewThemeSubmissions)]
    public IActionResult GetAwaitingApprovalThemes(int page = 1, int perPage = 50, string filters = "", string order = "", string search = "")
    {
        PaginationDto paginationDto = new(page, perPage, filters, order, search);
        PaginatedResponse<CssTheme> response = _theme.GetNonApprovedThemes(paginationDto);
        return response.Ok();
    }

    [HttpGet("ids")]
    public IActionResult GetThemes(string ids) // ids is split on `.`
    {
        List<string> idsList = ids.Split('.').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

        if (idsList.Count <= 0)
            return new List<string>().Ok();

        return _theme.GetThemesByIds(idsList, false).Select(x => ((IToDto<MinimalCssThemeDto>)x).ToDto()).Ok();
    }

    [HttpGet("{id}")]
    public IActionResult GetTheme(string id)
        => ((IToDto<FullCssThemeDto>)_theme.GetThemeById(id, false).Require("Theme not found")).ToDto().Ok();

    [HttpPatch("{id}")]
    [Authorize]
    [JwtRoleRequire(Permissions.EditAnyPost)]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult EditTheme(string id, CssThemeDirectPatchDto patch)
    {
        CssTheme theme = _theme.GetThemeById(id).Require();
        User? author = (patch.Author == null) ? null : _user.GetActiveUserById(patch.Author).Require();
        _theme.EditTheme(theme, patch.Description, patch.Target, author);
        return new OkResult();
    }

    [HttpDelete("{id}")]
    [Authorize]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult DeleteTheme(string id)
    {
        UserJwtDto jwt = _jwt.DecodeToken(Request).Require();
        CssTheme theme = _theme.GetThemeById(id).Require();

        if (!jwt.HasPermission(Permissions.EditAnyPost) && theme.Author.Id != jwt.Id)
            throw new NotFoundException("Could not find theme");
        
        _theme.DeleteTheme(theme, true, true);
        return new OkResult();
    }

    [HttpGet("legacy/audio")]
    public IActionResult GetAudioPacksAsLegacy(bool approved = true)
        =>  _theme.GetThemesLegacy(ThemeType.Audio, approved ? PostVisibility.Public : PostVisibility.Private).Ok();

    [HttpGet("legacy/css")]
    public IActionResult GetCssThemesAsLegacy(bool approved = true)
        =>  _theme.GetThemesLegacy(ThemeType.Css, approved ? PostVisibility.Public : PostVisibility.Private).Ok();
}