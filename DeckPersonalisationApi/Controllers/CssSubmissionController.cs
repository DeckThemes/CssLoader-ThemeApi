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
[Route("css_submissions")]
public class CssSubmissionController : Controller
{
    private JwtService _jwt;
    private CssThemeService _css;
    private CssSubmissionService _submission;
    private UserService _user;
    private BlobService _blob;
    
    public CssSubmissionController(JwtService jwt, CssThemeService css, CssSubmissionService submission, UserService user, BlobService blob)
    {
        _jwt = jwt;
        _css = css;
        _submission = submission;
        _user = user;
        _blob = blob;
    }

    [HttpPost("git")]
    [Authorize]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult SubmitThemeViaGit(CssThemeGitSubmitPostDto post)
    {
        UserJwtDto dto = _jwt.DecodeToken(Request).Require("Could not find user");
        User user = _user.GetActiveUserById(dto.Id).Require("Could not find user");

        string task = _css.SubmitThemeViaGit(post.Url, string.IsNullOrWhiteSpace(post.Commit) ? null : post.Commit,
            post.Subfolder, user, post.Meta);

        return new OkObjectResult(new TaskIdGetDto(task));
    }
    
    [HttpPost("zip")]
    [Authorize]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult SubmitThemeViaZip(CssThemeZipSubmissionPostDto post)
    {
        UserJwtDto dto = _jwt.DecodeToken(Request).Require("Could not find user");
        SavedBlob blob = _blob.GetBlob(post.Blob).Require("Could not find blob");

        if (blob.Confirmed || blob.Deleted)
            throw new BadRequestException("Can't use a blob that's already used");

        if (blob.Owner.Id != dto.Id)
            throw new UnauthorisedException("Can't use a blob from someone else");

        User user = _user.GetUserById(dto.Id).Require("Could not find user");
        _blob.ConfirmBlob(blob);

        string task = _css.SubmitThemeViaZip(blob, post.Meta, user);
        return new OkObjectResult(new TaskIdGetDto(task));
    }
    
    [HttpPost("css")]
    [Authorize]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult SubmitThemeViaCss(CssThemeCssSubmissionPostDto post)
    {
        UserJwtDto dto = _jwt.DecodeToken(Request).Require("Could not find user");
        User user = _user.GetActiveUserById(dto.Id).Require("Could not find user");

        string task = _css.SubmitThemeViaCss(post.Css, post.Name, post.Meta, user);
        return new OkObjectResult(new TaskIdGetDto(task));
    }
    
    [HttpGet]
    [Authorize]
    [JwtRoleRequire(Permissions.ViewThemeSubmissions)]
    public IActionResult ViewSubmissions(int page = 1, int perPage = 50, string filters = "", string order = "", string search = "")
    {
        PaginationDto pagination = new(page, perPage, filters, order, search);
        return new OkObjectResult(_submission.GetSubmissions(pagination).ToDto());
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
    public IActionResult GetSubmissionViaId(string id)
    {
        UserJwtDto jwt = _jwt.DecodeToken(Request).Require();
        CssSubmission submission = _submission.GetSubmissionById(id).Require("Could not find theme submission");

        if (jwt.HasPermission(Permissions.ViewThemeSubmissions) || jwt.Id == submission.Owner.Id)
            return new OkObjectResult(submission.ToDto());

        throw new NotFoundException("Could not find theme submission");
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
    public IActionResult DenyThemeSubmission(string id, MessageDto messageDto)
    {
        var token = _jwt.DecodeToken(Request).Require();
        User user = _user.GetUserById(token.Id).Require();

        if ((token.Permissions & Permissions.ApproveThemeSubmissions) != Permissions.ApproveThemeSubmissions)
        {
            CssSubmission submission = _submission.GetSubmissionById(id).Require("Could not find submission");
            if (submission.Owner.Id != user.Id)
                throw new UnauthorisedException("Unauthorized");
        }
        
        _submission.DenyCssTheme(id, messageDto.Message, user);
        return new OkResult();
    }
}