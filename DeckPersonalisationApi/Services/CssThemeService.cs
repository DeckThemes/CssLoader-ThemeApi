using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Extensions;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto.External.POST;
using DeckPersonalisationApi.Model.Dto.Internal.GET;
using DeckPersonalisationApi.Services.Css;
using DeckPersonalisationApi.Services.Tasks;
using DeckPersonalisationApi.Services.Tasks.Common;
using Microsoft.EntityFrameworkCore;

namespace DeckPersonalisationApi.Services;

public class CssThemeService
{
    private TaskService _task;
    private ApplicationContext _ctx;
    private BlobService _blob;
    private UserService _user;
    private AppConfiguration _config;

    public List<string> Targets => _config.CssTargets;

    public CssThemeService(TaskService task, ApplicationContext ctx, BlobService blob, UserService user, AppConfiguration config)
    {
        _task = task;
        _ctx = ctx;
        _blob = blob;
        _user = user;
        _config = config;
    }

    public string SubmitThemeViaGit(string url, string? commit, string subfolder, User user, CssSubmissionMeta meta)
    {
        Checks(user, meta);

        CreateTempFolderTask gitContainer = new CreateTempFolderTask();
        CloneGitTask clone = new CloneGitTask(url, commit, gitContainer, true);
        FolderSizeConstraintTask size = new FolderSizeConstraintTask(clone, _config.MaxCssThemeSize);
        PathTransformTask folder = new PathTransformTask(clone, subfolder);
        CopyFileTask copy = new CopyFileTask(clone, folder, "LICENSE");
        GetJsonTask jsonGet = new GetJsonTask(folder, "theme.json");
        ValidateCssThemeTask css = new ValidateCssThemeTask(folder, jsonGet, user, _config.CssTargets);
        WriteJsonTask jsonWrite = new WriteJsonTask(folder, "theme.json", jsonGet);
        CreateTempFolderTask themeContainer = new CreateTempFolderTask();
        CreateFolderTask themeFolder = new CreateFolderTask(themeContainer, css);
        CopyFileTask copyToThemeFolder = new CopyFileTask(folder, themeFolder, "*");
        ZipTask zip = new ZipTask(themeContainer, gitContainer);
        WriteAsBlobTask blob = new WriteAsBlobTask(user, zip);
        CreateCssSubmissionTask submission = new CreateCssSubmissionTask(css, blob, meta, url, user);

        List<ITaskPart> taskParts = new()
        {
            gitContainer, clone, size, folder, copy, jsonGet, css, jsonWrite, themeContainer, themeFolder, copyToThemeFolder, zip, blob, submission
        };

        AppTaskFromParts task = new(taskParts, "Submit theme via git", user);
        return _task.RegisterTask(task);
    }

    public string SubmitThemeViaZip(SavedBlob blob, CssSubmissionMeta meta, User user)
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

        AppTaskFromParts task = new(taskParts, "Submit theme via zip", user);
        return _task.RegisterTask(task);
    }

    public string SubmitThemeViaCss(string cssContent, string name, CssSubmissionMeta meta, User user)
    {
        Checks(user, meta);

        CreateTempFolderTask zipContainer = new CreateTempFolderTask();
        WriteStringToFileTask writeCss = new WriteStringToFileTask(zipContainer, "shared.css", cssContent);
        WriteStringToFileTask writeJson = new WriteStringToFileTask(zipContainer, "theme.json", CreateCssJson(name));
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
            zipContainer, writeCss, writeJson, size, jsonGet, css, jsonWrite, themeContainer, themeFolder, copyToThemeFolder, zip, blobSave, submission
        };

        AppTaskFromParts task = new(taskParts, "Submit theme via css", user);
        return _task.RegisterTask(task);
    }

    private void Checks(User user, CssSubmissionMeta meta)
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
    
    public CssTheme CreateTheme(string id, string name, List<string> imageIds, string blobId, string version,
        string? source, string authorId, string target, int manifestVersion, string description,
        List<string> dependencyNames, string specifiedAuthor)
    {
        _ctx.ChangeTracker.Clear();
        
        if (GetThemeById(id) != null)
            throw new Exception("Theme already exists");
        
        User author = _user.GetActiveUserById(authorId).Require("User not found");
        List<SavedBlob> imageBlobs = _blob.GetBlobs(imageIds).ToList();
        SavedBlob blob = _blob.GetBlob(blobId).Require();
        List<CssTheme> dependencies = _ctx.CssThemes.Where(x => dependencyNames.Contains(x.Name)).ToList();

        _blob.ConfirmBlobs(imageBlobs);
        _blob.ConfirmBlob(blob);

        CssTheme newTheme = new()
        {
            Id = id,
            Name = name,
            Images = imageBlobs,
            Download = blob,
            Version = version,
            Source = source,
            Author = author,
            Submitted = DateTimeOffset.Now,
            Updated = DateTimeOffset.Now,
            SpecifiedAuthor = specifiedAuthor,
            Target =  target,
            ManifestVersion = manifestVersion,
            Description = description,
            Dependencies = dependencies,
            Approved = false,
        };

        _ctx.CssThemes.Add(newTheme);
        _ctx.SaveChanges();

        return newTheme;
    }

    public void ApplyThemeUpdate(CssTheme original, CssTheme overlay)
    {
        original = GetThemeById(original.Id).Require("Failed to find original theme");
        overlay = GetThemeById(overlay.Id).Require("Failed to find update theme patch");
        
        if (original.Author.Id != overlay.Author.Id)
            throw new BadRequestException("Cannot overlay theme from another author");
        
        if (original.Download.Id != overlay.Download.Id)
            _blob.DeleteBlob(original.Download);

        _blob.DeleteBlobs(original.Images.Where(x => overlay.Images.All(y => y.Id != x.Id)).ToList());
        
        original.Images = overlay.Images;
        original.Download = overlay.Download;
        original.Version = overlay.Version;
        original.Source = overlay.Source;
        original.Updated = overlay.Updated;
        original.SpecifiedAuthor = overlay.SpecifiedAuthor;
        original.Target = overlay.Target;
        original.ManifestVersion = overlay.ManifestVersion;
        original.Description = overlay.Description;
        original.Dependencies = overlay.Dependencies;
        
        _ctx.CssThemes.Update(original);
        _ctx.SaveChanges();
    }

    public void DeleteTheme(CssTheme theme, bool deleteImages = false, bool deleteDownload = false)
    {
        theme = GetThemeById(theme.Id).Require("Could not find theme");
        theme.Deleted = true;
        _ctx.CssThemes.Update(theme);
        if (deleteImages)
            _blob.DeleteBlobs(theme.Images);
        
        if (deleteDownload)
            _blob.DeleteBlob(theme.Download);
        
        _ctx.SaveChanges();
    }

    public void ApproveTheme(CssTheme theme)
    {
        theme = GetThemeById(theme.Id).Require("Could not find theme");
        theme.Approved = true;
        _ctx.CssThemes.Update(theme);
        _ctx.SaveChanges();
    }

    public CssTheme? GetThemeById(string id)
        => _ctx.CssThemes
            .Include(x => x.Dependencies)
            .Include(x => x.Author)
            .Include(x => x.Download)
            .Include(x => x.Images)
            .FirstOrDefault(x => x.Id == id);
    
    public bool ThemeNameExists(string name)
        => _ctx.CssThemes.Any(x => x.Name == name && x.Approved & !x.Deleted);

    public IEnumerable<CssTheme> GetThemesByName(List<string> names)
        => _ctx.CssThemes.Include(x => x.Author)
            .Where(x => names.Contains(x.Name) && x.Approved && !x.Deleted).ToList();

    public PaginatedResponse<CssTheme> GetUsersThemes(User user, PaginationDto pagination)
        => GetThemesInternal(pagination, x => x.Where(y => y.Author == user && y.Approved));

    public PaginatedResponse<CssTheme> GetApprovedThemes(PaginationDto pagination)
        => GetThemesInternal(pagination, x => x.Where(y => y.Approved));
    
    public PaginatedResponse<CssTheme> GetNonApprovedThemes(PaginationDto pagination)
        => GetThemesInternal(pagination, x => x.Where(y => !y.Approved));

    public IEnumerable<string> Orders() => new List<string>()
    {
        "Alphabetical (A to Z)",
        "Alphabetical (Z to A)",
        "Last Updated",
        "First Updated",
        "Most Downloaded",
        "Least Downloaded"
    };

    private PaginatedResponse<CssTheme> GetThemesInternal(PaginationDto pagination, Func<IEnumerable<CssTheme>, IEnumerable<CssTheme>> middleware)
    {
        IEnumerable<CssTheme> part1 = _ctx.CssThemes
            .Include(x => x.Author)
            .Include(x => x.Download)
            .Include(x => x.Images);

        part1 = middleware(part1);
        part1 = part1.Where(x => ((pagination.Filters.Count <= 0) || pagination.Filters.Contains(x.Target)) && !x.Deleted);

        if (!string.IsNullOrWhiteSpace(pagination.Search))
            part1 = part1.Where(x => (x.Name.ToLower().Contains(pagination.Search)));
        
        switch (pagination.Order)
        {
            case "Alphabetical (A to Z)":
                part1 = part1.OrderByDescending(x => x.Name);
                break;
            case "Alphabetical (Z to A)":
                part1 = part1.OrderBy(x => x.Name);
                break;
            case "":
            case "Last Updated":
                part1 = part1.OrderByDescending(x => x.Updated);
                break;
            case "First Updated":
                part1 = part1.OrderBy(x => x.Updated);
                break;
            case "Most Downloaded":
                part1 = part1.OrderByDescending(x => x.Download.DownloadCount);
                break;
            case "Least Downloaded":
                part1 = part1.OrderByDescending(x => x.Download.DownloadCount);
                break;
            default:
                throw new BadRequestException($"Order type '{pagination.Order}' not found");
        }
        
        return new(part1.Count(), part1.Skip((pagination.Page - 1) * pagination.PerPage).Take(pagination.PerPage).ToList());
    }

    private string CreateCssJson(string name)
        => _config.CssToThemeJson.Replace("%THEME_NAME%", name.Replace("\"", "\\\""));
}