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

    [HttpPost("submit/git")]
    [Authorize]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult SubmitThemeViaGit(CssThemeGitSubmitPostDto post)
    {
        UserJwtDto dto = _jwt.DecodeToken(Request)!;

        string task = _service.SubmitThemeViaGit(post.Url, string.IsNullOrWhiteSpace(post.Commit) ? null : post.Commit,
            post.Subfolder, dto.Id);

        return new OkObjectResult(new TaskIdGetDto(task));
    }

    [HttpGet("approved")]
    public IActionResult GetThemes(int page = 1, int perPage = 50, string filters = "", string order = "")
    {
        PaginationDto paginationDto = new(page, perPage, filters, order);
        PaginatedResponse<CssTheme> response = _service.GetApprovedThemes(paginationDto);
        return new OkObjectResult(response.ToDto());
    }
    
    [HttpGet("approved/filters")]
    public IActionResult GetThemesFilters()
    {
        return new OkObjectResult(new PaginationFilters(_service.Targets, _service.Orders().ToList()));
    }

    [HttpGet("unapproved")]
    [Authorize]
    [JwtRoleRequire(Permissions.ViewThemeSubmissions)]
    public IActionResult GetUnapprovedThemes(int page = 1, int perPage = 50, string filters = "", string order = "")
    {
        PaginationDto paginationDto = new(page, perPage, filters, order);
        PaginatedResponse<CssTheme> response = _service.GetNonApprovedThemes(paginationDto);
        return new OkObjectResult(response.ToDto());
    }
    
    [HttpGet("unapproved/filters")]
    [Authorize]
    [JwtRoleRequire(Permissions.ViewThemeSubmissions)]
    public IActionResult GetUnapprovedThemesFilters()
    {
        return new OkResult();
    }

    [HttpGet("{id}")]
    public IActionResult GetTheme(string id)
    {
        return new OkResult();
    }
}