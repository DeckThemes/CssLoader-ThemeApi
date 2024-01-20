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
[Route("submissions")]
public class SubmissionController : Controller
{
    private JwtService _jwt;
    private SubmissionService _submission;
    private UserService _user;
    private BlobService _blob;
    private AppConfiguration _config;
    private ThemeService _themes;
    
    public SubmissionController(JwtService jwt, SubmissionService submission, UserService user, BlobService blob, AppConfiguration config, ThemeService themes)
    {
        _jwt = jwt;
        _submission = submission;
        _user = user;
        _blob = blob;
        _config = config;
        _themes = themes;
    }

    [HttpPost("css_git")]
    [Authorize]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult SubmitCssThemeViaGit(GitSubmitPostDto post)
    {
        UserJwtDto dto = _jwt.DecodeToken(Request).Require("Could not find user");
        User user = _user.GetActiveUserById(dto.Id).Require("Could not find user");

        ValidateMeta(user, post.Meta, ThemeType.Css);
        string task = _submission.SubmitCssThemeViaGit(post.Url, string.IsNullOrWhiteSpace(post.Commit) ? null : post.Commit,
            post.Subfolder, user, post.Meta);

        return new TaskIdGetDto(task).Ok();
    }
    
    [HttpPost("audio_git")]
    [Authorize]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult SubmitAudioPackViaGit(GitSubmitPostDto post)
    {
        UserJwtDto dto = _jwt.DecodeToken(Request).Require("Could not find user");
        User user = _user.GetActiveUserById(dto.Id).Require("Could not find user");

        ValidateMeta(user, post.Meta, ThemeType.Audio);
        string task = _submission.SubmitAudioPackViaGit(post.Url, string.IsNullOrWhiteSpace(post.Commit) ? null : post.Commit,
            post.Subfolder, user, post.Meta);

        return new TaskIdGetDto(task).Ok();
    }
    
    [HttpPost("css_zip")]
    [Authorize]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult SubmitCssThemeViaZip(ZipSubmissionPostDto post)
    {
        UserJwtDto dto = _jwt.DecodeToken(Request).Require("Could not find user");
        SavedBlob blob = _blob.GetBlob(post.Blob).Require("Could not find blob");

        if (blob.Confirmed || blob.Deleted)
            throw new BadRequestException("Can't use a blob that's already used");

        if (blob.Owner.Id != dto.Id)
            throw new UnauthorisedException("Can't use a blob from someone else");

        User user = _user.GetActiveUserById(dto.Id).Require("Could not find user");
        ValidateMeta(user, post.Meta, ThemeType.Css);
        
        _blob.ConfirmBlob(blob);
        string task = _submission.SubmitCssThemeViaZip(blob, post.Meta, user);
        return new TaskIdGetDto(task).Ok();
    }
    
    [HttpPost("audio_zip")]
    [Authorize]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult SubmitAudioPackViaZip(ZipSubmissionPostDto post)
    {
        UserJwtDto dto = _jwt.DecodeToken(Request).Require("Could not find user");
        SavedBlob blob = _blob.GetBlob(post.Blob).Require("Could not find blob");

        if (blob.Confirmed || blob.Deleted)
            throw new BadRequestException("Can't use a blob that's already used");

        if (blob.Owner.Id != dto.Id)
            throw new UnauthorisedException("Can't use a blob from someone else");

        User user = _user.GetActiveUserById(dto.Id).Require("Could not find user");
        ValidateMeta(user, post.Meta, ThemeType.Audio);
        
        _blob.ConfirmBlob(blob);
        string task = _submission.SubmitAudioPackViaZip(blob, post.Meta, user);
        return new TaskIdGetDto(task).Ok();
    }
    
    [HttpPost("css_css")]
    [Authorize]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult SubmitThemeViaCss(CssThemeCssSubmissionPostDto post)
    {
        UserJwtDto dto = _jwt.DecodeToken(Request).Require("Could not find user");
        User user = _user.GetActiveUserById(dto.Id).Require("Could not find user");
        
        ValidateMeta(user, post.Meta, ThemeType.Css);

        if (post.Name.Length >= _config.MaxNameLength)
            throw new BadRequestException($"Name can be max {_config.MaxNameLength} characters");

        if (post.Css.Length >= _config.MaxCssOnlySubmissionSize)
            throw new BadRequestException($"Css body can only be max {_config.MaxCssOnlySubmissionSize} characters");

        string task = _submission.SubmitCssThemeViaCss(post.Css, post.Name, post.Meta, user);
        return new TaskIdGetDto(task).Ok();
    }
    
    [HttpGet]
    [Authorize]
    [JwtRoleRequire(Permissions.ViewThemeSubmissions)]
    public IActionResult ViewSubmissions(int page = 1, int perPage = 50, string filters = "", string order = "", string search = "")
    {
        PaginationDto pagination = new(page, perPage, filters, order, search);
        return _submission.GetSubmissions(pagination).Ok();
    }
    
    [HttpGet("filters")]
    [Authorize]
    [JwtRoleRequire(Permissions.ViewThemeSubmissions)]
    public IActionResult ViewSubmissionsFilters(string type = "")
    {
        ThemeType? themeType = null;

        if (type.ToLower() == "css")
            themeType = ThemeType.Css;
        else if (type.ToLower() == "audio")
            themeType = ThemeType.Audio;
        
        return new OkObjectResult(new PaginationFilters(_submission.FiltersWithCount(themeType, null), _submission.Orders().ToList()));
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
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult ApproveThemeSubmissions(string id, MessageDto messageDto)
    {
        User user = _user.GetActiveUserById(_jwt.DecodeToken(Request).Require().Id).Require();
        _submission.ApproveTheme(id, messageDto.Message, user);
        return new OkResult();
    }

    [HttpPut("{id}/deny")]
    [Authorize]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult DenyThemeSubmission(string id, MessageDto messageDto)
    {
        var token = _jwt.DecodeToken(Request).Require();
        User user = _user.GetActiveUserById(token.Id).Require();

        if ((token.Permissions & Permissions.ApproveThemeSubmissions) != Permissions.ApproveThemeSubmissions)
        {
            CssSubmission submission = _submission.GetSubmissionById(id).Require("Could not find submission");
            if (submission.Owner.Id != user.Id)
                throw new UnauthorisedException("Unauthorized");
        }
        
        _submission.DenyTheme(id, messageDto.Message, user);
        return new OkResult();
    }

    private void ValidateMeta(User user, SubmissionMeta meta, ThemeType type)
    {
        if (!meta.PrivateSubmission)
            CheckIfUserIsAllowedToMakeSubmission(user);
        
        CheckImageBlobs(user, meta.ImageBlobs);
        ValidateMetaDescription(meta.Description);
        
        if (type == ThemeType.Css)
            ValidateMetaTarget(meta.Target);

        if (meta.PrivateSubmission && DiscordBot.Instance.PermissionStateOfUser(user.Id) == "None")
            throw new BadRequestException("Can only create private themes on premium account");
        
        if (_themes.GetThemeCountOfUser(user) >= _config.MaxThemeCount)
            throw new BadRequestException("Theme limit reached");
    }
    
    private void CheckIfUserIsAllowedToMakeSubmission(User user)
    {
        if (_user.GetSubmissionCountByUser(user, SubmissionStatus.AwaitingApproval) >= _config.MaxActiveSubmissions)
            throw new BadRequestException(
                $"Cannot have more than {_config.MaxActiveSubmissions} submissions awaiting approval");
    }
    
    private void CheckImageBlobs(User user, List<string>? incoming = null)
    {
        if (incoming is not { Count: > 0 })
            return;
        
        if (incoming.Count > _config.MaxImagesPerSubmission)
            throw new BadRequestException($"Cannot have more than {_config.MaxImagesPerSubmission} images per submission");
        
        List<SavedBlob> blobs = _blob.GetBlobs(incoming).ToList();
        if (blobs.Any(x => x.Confirmed)) 
            throw new BadRequestException("Cannot use images that are already used elsewhere");

        if (blobs.Any(x => x.Type == BlobType.Zip))
            throw new BadRequestException("Cannot use zip as an image");

        if (blobs.Any(x => x.Owner.Id != user.Id))
            throw new BadRequestException("One or more provided images are not yours");
    }
    
    private void ValidateMetaTarget(string? target)
    {
        if (target == null)
            return;
        
        if (!AppConfiguration.CssTargets.Contains(target))
            throw new BadRequestException($"Invalid CSS target {target}");

        if (target is "Preset" or "Profile")
            throw new BadRequestException("Meta Target cannot be 'Profile'");
    }

    private void ValidateMetaDescription(string? description)
    {
        if (description == null)
            return;

        if (description.Length > _config.MaxDescriptionLength)
            throw new BadRequestException($"Descriptions can be max {_config.MaxDescriptionLength} characters");
    }
}