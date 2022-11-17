using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Model;
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
    private IConfiguration _config;

    public long MaxCssThemeSize => long.Parse(_config["Config:MaxCssThemeSize"]!);

    public CssThemeService(TaskService task, ApplicationContext ctx, BlobService blob, UserService user, IConfiguration config)
    {
        _task = task;
        _ctx = ctx;
        _blob = blob;
        _user = user;
        _config = config;
    }

    public string SubmitThemeViaGit(string url, string? commit, string subfolder, string userId)
    {
        User? user = _user.GetActiveUserById(userId);
        
        if (user == null)
            throw new UnauthorisedException("User not found");

        List<string> validThemeTargets = _config["Config:CssTargets"]!.Split(';').ToList();
        
        CreateTempFolderTask gitContainer = new CreateTempFolderTask();
        CloneGitTask clone = new CloneGitTask(url, commit, gitContainer, true);
        FolderSizeConstraintTask size = new FolderSizeConstraintTask(clone, MaxCssThemeSize);
        PathTransformTask folder = new PathTransformTask(clone, subfolder);
        CopyFileTask copy = new CopyFileTask(clone, folder, "LICENSE");
        GetJsonTask jsonGet = new GetJsonTask(folder, "theme.json");
        ValidateCssThemeTask css = new ValidateCssThemeTask(folder, jsonGet, user, validThemeTargets);
        WriteJsonTask jsonWrite = new WriteJsonTask(folder, "theme.json", jsonGet);
        CreateTempFolderTask themeContainer = new CreateTempFolderTask();
        CreateFolderTask themeFolder = new CreateFolderTask(themeContainer, css);
        CopyFileTask copyToThemeFolder = new CopyFileTask(folder, themeFolder, "*");
        ZipTask zip = new ZipTask(themeContainer, gitContainer);
        WriteAsBlobTask blob = new WriteAsBlobTask(user, zip);
        // TODO: ImageIds
        CreateCssSubmissionTask submission = new CreateCssSubmissionTask(css, blob, new(), url, user);

        List<ITaskPart> taskParts = new()
        {
            gitContainer, clone, size, folder, copy, jsonGet, css, jsonWrite, themeContainer, themeFolder, copyToThemeFolder, zip, blob, submission
        };

        AppTaskFromParts task = new(taskParts, "Submit theme via git", user);
        return _task.RegisterTask(task);
    }

    // TODO: Theme updates don't seem to work
    public CssSubmission CreateSubmission(string id, string name, List<string> imageIds, SavedBlob blob, string version,
        string? source, User author, string target, int manifestVersion, string description,
        List<string> dependencyNames)
    {
        author = _user.GetActiveUserById(author.Id);

        if (author == null)
            throw new BadRequestException("Author is null");
        
        CssTheme? theme = GetThemeById(id);
        List<CssTheme> dependencies = _ctx.CssThemes.Where(x => dependencyNames.Contains(x.Name)).ToList();
        List<SavedBlob> imageBlobs = _blob.GetBlobs(imageIds).ToList();
        _blob.ConfirmBlobs(imageBlobs);
        _blob.ConfirmBlob(blob);
        
        if (theme != null)
            id = Guid.NewGuid().ToString();

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
            Target = target,
            ManifestVersion = manifestVersion,
            Description = description,
            Dependencies = dependencies,
            Approved = false,
        };

        _ctx.CssThemes.Add(newTheme);

        CssSubmission submission = new()
        {
            Id = Guid.NewGuid().ToString(),
            Intent = (theme == null) ? CssSubmissionIntent.NewTheme : CssSubmissionIntent.UpdateTheme,
            Theme = theme ?? newTheme,
            ThemeUpdate = (theme != null) ? newTheme : null,
            Status = SubmissionStatus.AwaitingApproval
        };

        _ctx.CssSubmissions.Add(submission);
        _ctx.SaveChanges();
        return submission;
    }

    public CssTheme? GetThemeById(string id)
        => _ctx.CssThemes
            .Include(x => x.Dependencies)
            .Include(x => x.Author)
            .Include(x => x.Download)
            .Include(x => x.Images)
            .FirstOrDefault(x => x.Id == id);
    
    public bool ThemeNameExists(string name)
        => _ctx.CssThemes.Any(x => x.Name == name && x.Approved);

    public IEnumerable<CssTheme> GetThemesByName(List<string> names)
        => _ctx.CssThemes.Where(x => names.Contains(x.Name) && x.Approved).ToList();
    
    public IEnumerable<CssTheme> GetUsersThemes(User user)
        => _ctx.CssThemes.Where(x => x.Author == user && x.Approved).ToList();
    
    public IEnumerable<CssTheme> GetUsersThemes(User user, PaginationDto pagination)
        => _ctx.CssThemes.Where(x => x.Author == user && x.Approved).Skip((pagination.Page - 1) * pagination.PerPage).Take(pagination.PerPage).ToList();
}