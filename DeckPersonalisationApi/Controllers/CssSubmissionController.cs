using DeckPersonalisationApi.Middleware.JwtRole;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto.External.GET;
using DeckPersonalisationApi.Model.Dto.External.POST;
using DeckPersonalisationApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeckPersonalisationApi.Controllers;

[ApiController]
[Route("css_submissions")]
public class CssSubmissionController : Controller
{
    private JwtService _jwt;
    private CssThemeService _css;

    public CssSubmissionController(JwtService jwt, CssThemeService css)
    {
        _jwt = jwt;
        _css = css;
    }

    [HttpPost("git")]
    [Authorize]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult SubmitThemeViaGit(CssThemeGitSubmitPostDto post)
    {
        UserJwtDto dto = _jwt.DecodeToken(Request)!;

        string task = _css.SubmitThemeViaGit(post.Url, string.IsNullOrWhiteSpace(post.Commit) ? null : post.Commit,
            post.Subfolder, dto.Id);

        return new OkObjectResult(new TaskIdGetDto(task));
    }
    
    [HttpPost("zip")]
    [Authorize]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult SubmitThemeViaZip()
    {
        throw new NotImplementedException();
    }
    
    [HttpPost("css")]
    [Authorize]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult SubmitThemeViaCss()
    {
        throw new NotImplementedException();
    }
    
    [HttpGet]
    [Authorize]
    [JwtRoleRequire(Permissions.ViewThemeSubmissions)]
    public IActionResult ViewSubmissions(int page = 1, int perPage = 50, string filters = "", string order = "")
    {
        throw new NotImplementedException();
    }
    
    [HttpGet("filters")]
    [Authorize]
    [JwtRoleRequire(Permissions.ViewThemeSubmissions)]
    public IActionResult ViewSubmissionsFilters()
    {
        throw new NotImplementedException();
    }
}