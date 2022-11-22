using DeckPersonalisationApi.Extensions;
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
    public IActionResult GetCssThemes(string id, int page = 1, int perPage = 50, string filters = "", string order = "")
    {
        User user = _user.GetActiveUserById(id).Require();

        PaginationDto paginationDto = new(page, perPage, filters, order);
        PaginatedResponse<CssTheme> response = _css.GetUsersThemes(user, paginationDto);
        return new OkObjectResult(response.ToDto());
    }

    [HttpGet("me/css_themes")]
    [Authorize]
    public IActionResult GetCssThemesMe(int page = 1, int perPage = 50, string filters = "", string order = "")
    {
        UserJwtDto user = _jwt.DecodeToken(Request).Require();
        return GetCssThemes(user.Id, page, perPage, filters, order);
    }
    
    [HttpGet("{id}/css_themes/filters")]
    public IActionResult GetCssThemesFilters(string id)
    {
        return new OkObjectResult(new PaginationFilters(_css.Targets, _css.Orders().ToList()));
    }

    [HttpGet("{id}/css_submissions")]
    [Authorize]
    [JwtRoleRequire(Permissions.ViewThemeSubmissions)]
    public IActionResult GetCssSubmissions(string id, int page = 1, int perPage = 50, string filters = "", string order = "")
    {
        PaginationDto paginationDto = new(page, perPage, filters, order);
        User user = _user.GetActiveUserById(id).Require();
        return new OkObjectResult(_submission.GetSubmissionsFromUser(paginationDto, user));
    }

    [HttpGet("me/css_submissions")]
    [Authorize]
    public IActionResult GetCssSubmissionsMe(int page = 1, int perPage = 50, string filters = "", string order = "")
    {
        UserJwtDto user = _jwt.DecodeToken(Request).Require();
        return GetCssSubmissions(user.Id, page, perPage, filters, order);
    }
}