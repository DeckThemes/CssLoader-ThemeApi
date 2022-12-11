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
    private CssThemeService _service;

    public CssThemeController(JwtService jwt, CssThemeService service)
    {
        _jwt = jwt;
        _service = service;
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

    [HttpGet("legacy")]
    public IActionResult GetThemesAsLegacy()
    {
        return new OkObjectResult(_service.GetThemesLegacy());
    }
}