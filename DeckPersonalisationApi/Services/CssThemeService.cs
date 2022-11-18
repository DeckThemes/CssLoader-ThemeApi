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

    public List<string> Targets => _config["Config:CssTargets"]!.Split(';').ToList();

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

        CreateTempFolderTask gitContainer = new CreateTempFolderTask();
        CloneGitTask clone = new CloneGitTask(url, commit, gitContainer, true);
        FolderSizeConstraintTask size = new FolderSizeConstraintTask(clone, MaxCssThemeSize);
        PathTransformTask folder = new PathTransformTask(clone, subfolder);
        CopyFileTask copy = new CopyFileTask(clone, folder, "LICENSE");
        GetJsonTask jsonGet = new GetJsonTask(folder, "theme.json");
        ValidateCssThemeTask css = new ValidateCssThemeTask(folder, jsonGet, user, Targets);
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
        List<string> dependencyNames, string specifiedAuthor)
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
            Updated = DateTimeOffset.Now,
            SpecifiedAuthor = specifiedAuthor,
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
            Status = SubmissionStatus.AwaitingApproval,
            Submitted = DateTimeOffset.Now,
            Owner = author
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
        => _ctx.CssThemes.Any(x => x.Name == name && x.Approved & !x.Disabled);

    public IEnumerable<CssTheme> GetThemesByName(List<string> names)
        => _ctx.CssThemes.Where(x => names.Contains(x.Name) && x.Approved && !x.Disabled).ToList();
    
    public IEnumerable<CssTheme> GetUsersThemes(User user)
        => _ctx.CssThemes.Where(x => x.Author == user && x.Approved && !x.Disabled).ToList();

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
        part1 = part1.Where(x => ((pagination.Filters.Count <= 0) || pagination.Filters.Contains(x.Target)) && !x.Disabled);

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
}