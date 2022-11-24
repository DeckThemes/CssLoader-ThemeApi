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
[Route("css_submissions")]
public class CssSubmissionController : Controller
{
    private JwtService _jwt;
    private CssThemeService _css;
    private CssSubmissionService _submission;
    private UserService _user;
    
    public CssSubmissionController(JwtService jwt, CssThemeService css, CssSubmissionService submission, UserService user)
    {
        _jwt = jwt;
        _css = css;
        _submission = submission;
        _user = user;
    }

    [HttpPost("git")]
    [Authorize]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult SubmitThemeViaGit(CssThemeGitSubmitPostDto post)
    {
        UserJwtDto dto = _jwt.DecodeToken(Request)!;

        string task = _css.SubmitThemeViaGit(post.Url, string.IsNullOrWhiteSpace(post.Commit) ? null : post.Commit,
            post.Subfolder, dto.Id, post.Meta);

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
        PaginationDto pagination = new(page, perPage, filters, order);
        return new OkObjectResult(_submission.GetSubmissions(pagination));
    }
    
    [HttpGet("filters")]
    [Authorize]
    [JwtRoleRequire(Permissions.ViewThemeSubmissions)]
    public IActionResult ViewSubmissionsFilters()
    {
        return new OkObjectResult(new PaginationFilters(_submission.Filters().ToList(), _submission.Orders().ToList()));
    }

    [HttpGet("{id}")]
    [Authorize]
    [JwtRoleRequire(Permissions.ViewThemeSubmissions)]
    public IActionResult GetSubmissionViaId(string id)
    {
        return new OkObjectResult(_submission.GetSubmissionById(id).Require());
    }

    [HttpPut("{id}/approve")]
    [Authorize]
    [JwtRoleRequire(Permissions.ApproveThemeSubmissions)]
    public IActionResult ApproveThemeSubmissions(string id, MessageDto messageDto)
    {
        User user = _user.GetUserById(_jwt.DecodeToken(Request).Require().Id).Require();
        _submission.ApproveCssTheme(id, messageDto.Message, user);
        return new OkResult();
    }

    [HttpPut("{id}/deny")]
    [Authorize]
    [JwtRoleRequire(Permissions.ApproveThemeSubmissions)]
    public IActionResult DenyThemeSubmission(string id, MessageDto messageDto)
    {
        User user = _user.GetUserById(_jwt.DecodeToken(Request).Require().Id).Require();
        _submission.DenyCssTheme(id, messageDto.Message, user);
        return new OkResult();
    }
}