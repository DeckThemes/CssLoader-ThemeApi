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
[Route("css_themes")]
public class CssThemeController : Controller
{
    private JwtService _jwt;
    private UserService _user;
    private CssThemeService _service;

    public CssThemeController(JwtService jwt, CssThemeService service, UserService user)
    {
        _jwt = jwt;
        _service = service;
        _user = user;
    }

    [HttpGet]
    public IActionResult GetThemes(int page = 1, int perPage = 50, string filters = "", string order = "", string search = "")
    {
        PaginationDto paginationDto = new(page, perPage, filters, order, search);
        PaginatedResponse<CssTheme> response = _service.GetApprovedThemes(paginationDto);
        return new OkObjectResult(response.ToDto());
    }
    
    [HttpGet("filters")]
    [HttpGet("awaiting_approval/filters")]
    public IActionResult GetThemesFilters()
    {
        return new OkObjectResult(new PaginationFilters(_service.Targets, _service.Orders().ToList()));
    }

    [HttpGet("awaiting_approval")]
    [Authorize]
    [JwtRoleRequire(Permissions.ViewThemeSubmissions)]
    public IActionResult GetAwaitingApprovalThemes(int page = 1, int perPage = 50, string filters = "", string order = "", string search = "")
    {
        PaginationDto paginationDto = new(page, perPage, filters, order, search);
        PaginatedResponse<CssTheme> response = _service.GetNonApprovedThemes(paginationDto);
        return new OkObjectResult(response.ToDto());
    }

    [HttpGet("{id}")]
    public IActionResult GetTheme(string id)
    {
        CssTheme? theme = _service.GetThemeById(id);
        theme ??= _service.GetThemesByName(new() { id }).FirstOrDefault();
        theme.Require("Theme not found");
        
        return new OkObjectResult(((IToDto<FullCssThemeDto>)theme!).ToDto());
    }

    [HttpPatch("{id}")]
    [Authorize]
    [JwtRoleRequire(Permissions.EditAnyPost)]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult EditTheme(string id, CssThemeDirectPatchDto patch)
    {
        CssTheme theme = _service.GetThemeById(id).Require();
        User? author = (patch.Author == null) ? null : _user.GetActiveUserById(patch.Author).Require();
        _service.EditTheme(theme, patch.Description, patch.Target, author);
        return new OkResult();
    }

    [HttpDelete("{id}")]
    [Authorize]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult DeleteTheme(string id)
    {
        UserJwtDto jwt = _jwt.DecodeToken(Request).Require();
        CssTheme theme = _service.GetThemeById(id).Require();

        if (!jwt.HasPermission(Permissions.EditAnyPost) && theme.Author.Id != jwt.Id)
            throw new NotFoundException("Could not find theme");
        
        _service.DeleteTheme(theme, true, true);
        return new OkResult();
    }

    [HttpGet("legacy")]
    public IActionResult GetThemesAsLegacy()
    {
        return new OkObjectResult(_service.GetThemesLegacy());
    }
}