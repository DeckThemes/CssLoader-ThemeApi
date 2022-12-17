﻿using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Extensions;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto.External.GET;
using DeckPersonalisationApi.Model.Dto.External.POST;
using DeckPersonalisationApi.Model.Dto.Internal.GET;
using DeckPersonalisationApi.Services.Audio;
using DeckPersonalisationApi.Services.Css;
using DeckPersonalisationApi.Services.Tasks;
using DeckPersonalisationApi.Services.Tasks.Common;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DeckPersonalisationApi.Services;

public class SubmissionService
{
    private ApplicationContext _ctx;
    private ThemeService _themes;
    private BlobService _blob;
    private UserService _user;
    private AppConfiguration _config;
    private TaskService _task;
    
    public SubmissionService(ApplicationContext ctx, BlobService blob, ThemeService themes, UserService user, AppConfiguration config, TaskService task)
    {
        _ctx = ctx;
        _blob = blob;
        _themes = themes;
        _user = user;
        _config = config;
        _task = task;
    }
    
    public void ApproveTheme(string id, string? message, User reviewer)
    {
        CssSubmission submission = GetSubmissionById(id).Require("Failed to find submission");

        if (submission.Status != SubmissionStatus.AwaitingApproval)
            throw new BadRequestException("Submission is not awaiting approval");
        
        CssTheme newTheme = submission.New;
        CssTheme? oldTheme = submission.Old;
        
        newTheme = _themes.GetThemeById(newTheme.Id).Require("Failed to find new theme");
        oldTheme = (oldTheme == null) ? null : _themes.GetThemeById(oldTheme.Id).Require("Failed to find old theme");

        if (oldTheme == null)
            _themes.ApproveTheme(newTheme);
        else
        {
            _themes.ApplyThemeUpdate(oldTheme, newTheme);
            _themes.DeleteTheme(newTheme);
        }

        submission.ReviewedBy = reviewer;
        submission.Status = SubmissionStatus.Approved;
        submission.Message = message;
        _ctx.CssSubmissions.Update(submission);
        _ctx.SaveChanges();
        
        Utils.Utils.SendDiscordWebhook(_config, submission);
    }

    public void DenyTheme(string id, string? message, User reviewer)
    {
        CssSubmission submission = GetSubmissionById(id).Require("Failed to find submission");
        
        if (submission.Status != SubmissionStatus.AwaitingApproval)
            throw new BadRequestException("Submission is not awaiting approval");
        
        CssTheme newTheme = submission.New;
        CssTheme? oldTheme = submission.Old;

        newTheme = _themes.GetThemeById(newTheme.Id).Require("Failed to find new theme");
        oldTheme = (oldTheme == null) ? null : _themes.GetThemeById(oldTheme.Id).Require("Failed to find old theme");

        _themes.DeleteTheme(newTheme, oldTheme?.Images.All(x => newTheme.Images.Any(y => y.Id == x.Id)) ?? true, (oldTheme == null) || oldTheme.Download.Id == newTheme.Download.Id);
        
        submission.ReviewedBy = reviewer;
        submission.Status = SubmissionStatus.Denied;
        submission.Message = message;
        _ctx.CssSubmissions.Update(submission);
        _ctx.SaveChanges();
        
        Utils.Utils.SendDiscordWebhook(_config, submission);
    }
    
    public string SubmitCssThemeViaGit(string url, string? commit, string subfolder, User user, SubmissionMeta meta)
    {
        Checks(user, meta);

        CreateTempFolderTask gitContainer = new CreateTempFolderTask();
        CloneGitTask clone = new CloneGitTask(url, commit, gitContainer);
        PathTransformTask folder = new PathTransformTask(clone, subfolder);
        FolderSizeConstraintTask size = new FolderSizeConstraintTask(folder, _config.MaxCssThemeSize);
        CopyFileTask copy = new CopyFileTask(clone, folder, "LICENSE");
        GetJsonTask jsonGet = new GetJsonTask(folder, "theme.json");
        ValidateCssThemeTask css = new ValidateCssThemeTask(folder, jsonGet, user, _config.CssTargets);
        WriteJsonTask jsonWrite = new WriteJsonTask(folder, "theme.json", jsonGet);
        CreateTempFolderTask themeContainer = new CreateTempFolderTask();
        CreateFolderTask themeFolder = new CreateFolderTask(themeContainer, css);
        CopyFileTask copyToThemeFolder = new CopyFileTask(folder, themeFolder, "*");
        ZipTask zip = new ZipTask(themeContainer, gitContainer);
        WriteAsBlobTask blob = new WriteAsBlobTask(user, zip);
        CreateCssSubmissionTask submission = new CreateCssSubmissionTask(css, blob, meta, clone, user);

        List<ITaskPart> taskParts = new()
        {
            gitContainer, clone, folder, size, copy, jsonGet, css, jsonWrite, themeContainer, themeFolder, copyToThemeFolder, zip, blob, submission
        };

        AppTaskFromParts task = new(taskParts, "Submit css theme via git", user);
        return _task.RegisterTask(task);
    }

    public string SubmitCssThemeViaZip(SavedBlob blob, SubmissionMeta meta, User user)
    {
        Checks(user, meta);

        CreateTempFolderTask zipContainer = new CreateTempFolderTask();
        ExtractZipTask extractZip = new ExtractZipTask(zipContainer, blob, _config.MaxCssThemeSize);
        FolderSizeConstraintTask size = new FolderSizeConstraintTask(zipContainer, _config.MaxCssThemeSize);
        GetJsonTask jsonGet = new GetJsonTask(zipContainer, "theme.json");
        ValidateCssThemeTask css = new ValidateCssThemeTask(zipContainer, jsonGet, user, _config.CssTargets);
        WriteJsonTask jsonWrite = new WriteJsonTask(zipContainer, "theme.json", jsonGet);
        CreateTempFolderTask themeContainer = new CreateTempFolderTask();
        CreateFolderTask themeFolder = new CreateFolderTask(themeContainer, css);
        CopyFileTask copyToThemeFolder = new CopyFileTask(zipContainer, themeFolder, "*");
        ZipTask zip = new ZipTask(themeContainer, zipContainer);
        WriteAsBlobTask blobSave = new WriteAsBlobTask(user, zip);
        CreateCssSubmissionTask submission = new CreateCssSubmissionTask(css, blobSave, meta, "[Zip Deploy]", user);

        List<ITaskPart> taskParts = new()
        {
            zipContainer, extractZip, size, jsonGet, css, jsonWrite, themeContainer, themeFolder, copyToThemeFolder, zip, blobSave, submission
        };

        AppTaskFromParts task = new(taskParts, "Submit css theme via zip", user);
        return _task.RegisterTask(task);
    }

    public string SubmitCssThemeViaCss(string cssContent, string name, SubmissionMeta meta, User user)
    {
        Checks(user, meta);

        CreateTempFolderTask cssContainer = new CreateTempFolderTask();
        WriteStringToFileTask writeCss = new WriteStringToFileTask(cssContainer, "shared.css", cssContent);
        WriteStringToFileTask writeJson = new WriteStringToFileTask(cssContainer, "theme.json", CreateCssJson(name));
        FolderSizeConstraintTask size = new FolderSizeConstraintTask(cssContainer, _config.MaxCssThemeSize);
        GetJsonTask jsonGet = new GetJsonTask(cssContainer, "theme.json");
        ValidateCssThemeTask css = new ValidateCssThemeTask(cssContainer, jsonGet, user, _config.CssTargets);
        WriteJsonTask jsonWrite = new WriteJsonTask(cssContainer, "theme.json", jsonGet);
        CreateTempFolderTask themeContainer = new CreateTempFolderTask();
        CreateFolderTask themeFolder = new CreateFolderTask(themeContainer, css);
        CopyFileTask copyToThemeFolder = new CopyFileTask(cssContainer, themeFolder, "*");
        ZipTask zip = new ZipTask(themeContainer, cssContainer);
        WriteAsBlobTask blobSave = new WriteAsBlobTask(user, zip);
        CreateCssSubmissionTask submission = new CreateCssSubmissionTask(css, blobSave, meta, "[Css Only Deploy]", user);

        List<ITaskPart> taskParts = new()
        {
            cssContainer, writeCss, writeJson, size, jsonGet, css, jsonWrite, themeContainer, themeFolder, copyToThemeFolder, zip, blobSave, submission
        };

        AppTaskFromParts task = new(taskParts, "Submit css theme via css", user);
        return _task.RegisterTask(task);
    }
    
    public string SubmitAudioPackViaGit(string url, string? commit, string subfolder, User user, SubmissionMeta meta)
    {
        Checks(user, meta);

        CreateTempFolderTask gitContainer = new CreateTempFolderTask();
        CloneGitTask clone = new CloneGitTask(url, commit, gitContainer);
        PathTransformTask folder = new PathTransformTask(clone, subfolder);
        FolderSizeConstraintTask size = new FolderSizeConstraintTask(folder, _config.MaxAudioPackSize);
        CopyFileTask copy = new CopyFileTask(clone, folder, "LICENSE");
        GetJsonTask jsonGet = new GetJsonTask(folder, "pack.json");
        ValidateAudioPackTask audio = new ValidateAudioPackTask(folder, jsonGet, user, _config.AudioFiles);
        WriteJsonTask jsonWrite = new WriteJsonTask(folder, "pack.json", jsonGet);
        CreateTempFolderTask themeContainer = new CreateTempFolderTask();
        CreateFolderTask themeFolder = new CreateFolderTask(themeContainer, audio);
        CopyFileTask copyToThemeFolder = new CopyFileTask(folder, themeFolder, "*");
        ZipTask zip = new ZipTask(themeContainer, gitContainer);
        WriteAsBlobTask blob = new WriteAsBlobTask(user, zip);
        CreateAudioSubmissionTask submission = new CreateAudioSubmissionTask(audio, blob, meta, clone, user);

        List<ITaskPart> taskParts = new()
        {
            gitContainer, clone, folder, size, copy, jsonGet, audio, jsonWrite, themeContainer, themeFolder, copyToThemeFolder, zip, blob, submission
        };

        AppTaskFromParts task = new(taskParts, "Submit audio pack via git", user);
        return _task.RegisterTask(task);
    }
    
    public string SubmitAudioPackViaZip(SavedBlob blob, SubmissionMeta meta, User user)
    {
        Checks(user, meta);

        CreateTempFolderTask zipContainer = new CreateTempFolderTask();
        ExtractZipTask extractZip = new ExtractZipTask(zipContainer, blob, _config.MaxAudioPackSize);
        FolderSizeConstraintTask size = new FolderSizeConstraintTask(zipContainer, _config.MaxAudioPackSize);
        GetJsonTask jsonGet = new GetJsonTask(zipContainer, "pack.json");
        ValidateAudioPackTask audio = new ValidateAudioPackTask(zipContainer, jsonGet, user, _config.AudioFiles);
        WriteJsonTask jsonWrite = new WriteJsonTask(zipContainer, "pack.json", jsonGet);
        CreateTempFolderTask themeContainer = new CreateTempFolderTask();
        CreateFolderTask themeFolder = new CreateFolderTask(themeContainer, audio);
        CopyFileTask copyToThemeFolder = new CopyFileTask(zipContainer, themeFolder, "*");
        ZipTask zip = new ZipTask(themeContainer, zipContainer);
        WriteAsBlobTask blobSave = new WriteAsBlobTask(user, zip);
        CreateAudioSubmissionTask submission = new CreateAudioSubmissionTask(audio, blobSave, meta, "[Zip Deploy]", user);

        List<ITaskPart> taskParts = new()
        {
            zipContainer, extractZip, size, jsonGet, audio, jsonWrite, themeContainer, themeFolder, copyToThemeFolder, zip, blobSave, submission
        };

        AppTaskFromParts task = new(taskParts, "Submit audio pack via zip", user);
        return _task.RegisterTask(task);
    }

    private void Checks(User user, SubmissionMeta meta)
    {
        if ((meta.ImageBlobs?.Count ?? 0) > _config.MaxImagesPerSubmission)
            throw new BadRequestException($"Cannot have more than {_config.MaxImagesPerSubmission} images per submission");

        if (_user.GetSubmissionCountByUser(user, SubmissionStatus.AwaitingApproval) > _config.MaxActiveSubmissions)
            throw new BadRequestException(
                $"Cannot have more than {_config.MaxActiveSubmissions} submissions awaiting approval");
        
        List<string>? possibleImageBlobs = meta.ImageBlobs;
        if (possibleImageBlobs != null && _blob.GetBlobs(possibleImageBlobs).Any(x => x.Confirmed)) 
            throw new BadRequestException("Cannot use images that are already used elsewhere");
    }

    public CssSubmission CreateSubmission(string? oldThemeId, string newThemeId, CssSubmissionIntent intent,
        string authorId, List<string> errors)
    {
        _ctx.ChangeTracker.Clear();
        User author = _user.GetActiveUserById(authorId).Require("User not found");
        CssTheme? oldTheme = (oldThemeId == null) ? null : _themes.GetThemeById(oldThemeId).Require();
        CssTheme newTheme = _themes.GetThemeById(newThemeId).Require();
        
        if ((intent != CssSubmissionIntent.NewTheme && oldTheme == null) || newTheme == null || author == null)
            throw new Exception("Intent validation failed");
        
        CssSubmission submission = new()
        {
            Id = Guid.NewGuid().ToString(),
            Intent = intent,
            Old = oldTheme,
            New = newTheme,
            Status = SubmissionStatus.AwaitingApproval,
            Submitted = DateTimeOffset.Now,
            Owner = author,
            Errors = JsonConvert.SerializeObject(errors)
        };

        _ctx.CssSubmissions.Add(submission);
        _ctx.SaveChanges();
        
        Utils.Utils.SendDiscordWebhook(_config, submission);
        
        return submission;
    }

    public IEnumerable<string> Orders() => new List<string>()
    {
        "Last to First",
        "First to Last",
    };

    public IEnumerable<string> Filters() => new List<string>()
    {
        SubmissionStatus.Approved.ToString(),
        SubmissionStatus.Denied.ToString(),
        SubmissionStatus.AwaitingApproval.ToString()
    };

    public CssSubmission? GetSubmissionById(string id)
        => _ctx.CssSubmissions
            .Include(x => x.ReviewedBy)
            .Include(x => x.Owner)
            .Include(x => x.New)
            .Include(x => x.New.Dependencies)
            .Include(x => x.New.Author)
            .Include(x => x.New.Download)
            .Include(x => x.New.Images)
            .Include(x => x.Old)
            .FirstOrDefault(x => x.Id == id);
    
    public PaginatedResponse<CssSubmission> GetSubmissions(PaginationDto pagination)
        => GetSubmissionsInternal(pagination, x => x);

    public PaginatedResponse<CssSubmission> GetSubmissionsFromUser(PaginationDto pagination, User user)
        => GetSubmissionsInternal(pagination, x => x.Where(y => y.Owner == user));

    private PaginatedResponse<CssSubmission> GetSubmissionsInternal(PaginationDto pagination, Func<IEnumerable<CssSubmission>, IEnumerable<CssSubmission>> middleware)
    {
        List<SubmissionStatus> status =
            pagination.Filters.Select(x =>
            {
                SubmissionStatus? a = null;
                if (Enum.TryParse(x, true, out SubmissionStatus res))
                    a = res;
                return a;
            }).Where(x => x != null).Select(x => x!.Value).ToList();

        IEnumerable<CssSubmission> part1 = _ctx.CssSubmissions
            .Include(x => x.ReviewedBy)
            .Include(x => x.Owner)
            .Include(x => x.New)
            .Include(x => x.New.Dependencies)
            .Include(x => x.New.Author)
            .Include(x => x.New.Download)
            .Include(x => x.New.Images)
            .Include(x => x.Old);

        part1 = middleware(part1);
        part1 = part1.Where(x => ((status.Count <= 0) || status.Contains(x.Status)));
        if (!string.IsNullOrWhiteSpace(pagination.Search))
            part1 = part1.Where(x => (x.New.Name.ToLower().Contains(pagination.Search)));
        
        if (pagination.Filters.Contains("CSS"))
            part1 = part1.Where(x => x.New.Type == ThemeType.Css);
        else if (pagination.Filters.Contains("AUDIO"))
            part1 = part1.Where(x => x.New.Type == ThemeType.Audio);
        
        switch (pagination.Order)
        {
            case "":
            case "Last to First":
                part1 = part1.OrderByDescending(x => x.Submitted);
                break;
            case "First to Last":
                part1 = part1.OrderBy(x => x.Submitted);
                break;
            default:
                throw new BadRequestException($"Order type '{pagination.Order}' not found");
        }
        
        return new(part1.Count(), part1.Skip((pagination.Page - 1) * pagination.PerPage).Take(pagination.PerPage).ToList());
    }
    
    private string CreateCssJson(string name)
        => _config.CssToThemeJson.Replace("%THEME_NAME%", name.Replace("\"", "\\\""));
}