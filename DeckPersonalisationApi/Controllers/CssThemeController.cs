using DeckPersonalisationApi.Middleware.JwtRole;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto.External.GET;
using DeckPersonalisationApi.Model.Dto.External.POST;
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
}