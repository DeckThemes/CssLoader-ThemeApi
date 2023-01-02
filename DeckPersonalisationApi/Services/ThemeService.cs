using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Extensions;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto.External.GET;
using DeckPersonalisationApi.Model.Dto.External.POST;
using DeckPersonalisationApi.Model.Dto.Internal.GET;
using DeckPersonalisationApi.Services.Css;
using DeckPersonalisationApi.Services.Tasks;
using DeckPersonalisationApi.Services.Tasks.Common;
using Microsoft.EntityFrameworkCore;

namespace DeckPersonalisationApi.Services;

public class ThemeService
{
    private TaskService _task;
    private ApplicationContext _ctx;
    private BlobService _blob;
    private UserService _user;
    private AppConfiguration _config;

    public List<string> CssTargets => _config.CssTargets;
    public List<string> AudioTargets => new() { "Music", "Audio" };

    public ThemeService(TaskService task, ApplicationContext ctx, BlobService blob, UserService user, AppConfiguration config)
    {
        _task = task;
        _ctx = ctx;
        _blob = blob;
        _user = user;
        _config = config;
    }

    public CssTheme CreateTheme(string id, string name, List<string> imageIds, string blobId, string version,
        string? source, string authorId, string target, int manifestVersion, string description,
        List<string> dependencyNames, string specifiedAuthor, ThemeType type)
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
            Type = type
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
        {
            overlay.Download.DownloadCount = original.Download.DownloadCount;
            _ctx.Blobs.Update(overlay.Download);
            _blob.DeleteBlob(original.Download);
        }
        
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

    public void EditTheme(CssTheme theme, string? description, string? target, User? author)
    {
        if (target != null)
        {
            if (!_config.CssTargets.Contains(target))
                throw new BadRequestException("Target is not a valid target type");
            
            theme.Target = target;
        }
        
        if (description != null)
            theme.Description = description;

        if (author != null)
        {
            theme.Author = author;
            theme.Download.Owner = author;
            theme.Images.ForEach(x => x.Owner = author);
        }

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

    public bool ThemeNameExists(string name, ThemeType type)
        => _ctx.CssThemes.Any(x => x.Name == name && x.Approved && !x.Deleted && x.Type == type);

    public List<LegacyThemesDto> GetThemesLegacy(ThemeType type, bool approved = true)
        => _ctx.CssThemes
            .Include(x => x.Images)
            .Include(x => x.Download)
            .Where(x => x.Type == type && x.Approved == approved && !x.Deleted)
            .ToList()
            .Select(x => new LegacyThemesDto(x, _config))
            .ToList();
    
    public IEnumerable<CssTheme> GetThemesByName(List<string> names, ThemeType type)
        => _ctx.CssThemes.Include(x => x.Author)
            .Where(x => x.Type == type).Where(x => names.Contains(x.Name) && x.Approved && !x.Deleted).ToList();
    
    public IEnumerable<CssTheme> GetAnyThemesByAuthorWithName(User user, string name, ThemeType type)
        => _ctx.CssThemes.Include(x => x.Author).Include(x => x.Images).Include(x => x.Download)
            .Where(x => x.Type == type).Where(x => x.Name == name && x.Author.Id == user.Id && !x.Deleted).ToList();

    public PaginatedResponse<CssTheme> GetUsersThemes(User user, PaginationDto pagination)
        => GetThemesInternal(pagination, x => x.Where(y => y.Author == user && y.Approved));

    public PaginatedResponse<CssTheme> GetApprovedThemes(PaginationDto pagination)
        => GetThemesInternal(pagination, x => x.Where(y => y.Approved));
    
    public PaginatedResponse<CssTheme> GetNonApprovedThemes(PaginationDto pagination)
        => GetThemesInternal(pagination, x => x.Where(y => !y.Approved));

    public PaginatedResponse<CssTheme> GetStarredThemesByUser(PaginationDto pagination, User user)
        => GetThemesInternal(pagination, x => x.Where(y => user.CssStars.Contains(y)));

    public void UpdateStars()
    {
        IEnumerable<Tuple<CssTheme, long>> themes = _ctx.CssThemes.Include(x => x.UserStars)
            .Select(x => new Tuple<CssTheme, long>(x, x.UserStars.Count));

        foreach (var (item1, item2) in themes)
        {
            if (item1.StarCount == item2)
                continue;
            
            item1.StarCount = item2;
            _ctx.CssThemes.Update(item1);
        }

        _ctx.SaveChanges();
    }

    public IEnumerable<string> Orders() => new List<string>()
    {
        "Alphabetical (A to Z)",
        "Alphabetical (Z to A)",
        "Last Updated",
        "First Updated",
        "Most Downloaded",
        "Least Downloaded",
        "Most Stars",
        "Least Stars"
    };

    public Dictionary<string, long> FiltersWithCount(ThemeType? type, User? user, bool stars = false, bool approved = true)
    {
        IQueryable<CssTheme> part1 = _ctx.CssThemes
            .Include(x => x.Author)
            .Where(x => !x.Deleted && x.Approved == approved);
            
        if (type != null)   
            part1 = part1.Where(x => x.Type == type.Value);

        if (user != null)
        {
            if (stars)
            {
                List<string> starIds = user.CssStars.Select(x => x.Id).ToList();
                part1 = part1.Where(x => starIds.Contains(x.Id));
                
            }
            else
            {
                part1 = part1.Where(x => x.Author.Id == user.Id);
            }
        }

        var items = part1
            .GroupBy(x => x.Target)
            .Select(x => new Tuple<string, long>(x.Key, x.Count()))
            .ToList()
            .OrderByDescending(x => x.Item2)
            .ToList();
        
        Dictionary<string, long> filters = new();
        items.ForEach(x => filters.Add(x.Item1, x.Item2));
        
        if (type == ThemeType.Css || type == null)
            CssTargets.ForEach(x =>
            {
                if (!filters.ContainsKey(x))
                    filters.Add(x, 0);
            });

        if (type == ThemeType.Audio || type == null)
            AudioTargets.ForEach(x =>
            {
                if (!filters.ContainsKey(x))
                    filters.Add(x, 0);
            });

        return filters;
    }

    private PaginatedResponse<CssTheme> GetThemesInternal(PaginationDto pagination, Func<IEnumerable<CssTheme>, IEnumerable<CssTheme>> middleware)
    {
        IEnumerable<CssTheme> part1 = _ctx.CssThemes
            .Include(x => x.Author)
            .Include(x => x.Download)
            .Include(x => x.Images);

        part1 = middleware(part1);
        
        if (pagination.Filters.Contains("CSS"))
            part1 = part1.Where(x => x.Type == ThemeType.Css);
        else if (pagination.Filters.Contains("AUDIO"))
            part1 = part1.Where(x => x.Type == ThemeType.Audio);
        
        List<string> filters = pagination.Filters.Where(x => x is not ("CSS" or "AUDIO")).Select(x => x.ToLower()).ToList();
        part1 = part1.Where(x => ((filters.Count <= 0) || filters.Contains(x.Target.ToLower())) && !x.Deleted);

        if (!string.IsNullOrWhiteSpace(pagination.Search))
            part1 = part1.Where(x => (x.Name.ToLower().Contains(pagination.Search)));
        
        switch (pagination.Order)
        {
            case "Alphabetical (A to Z)":
                part1 = part1.OrderBy(x => x.Name);
                break;
            case "Alphabetical (Z to A)":
                part1 = part1.OrderByDescending(x => x.Name);
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
                part1 = part1.OrderBy(x => x.Download.DownloadCount);
                break;
            case "Most Stars":
                part1 = part1.OrderByDescending(x => x.StarCount);
                break;
            case "Least Stars":
                part1 = part1.OrderBy(x => x.StarCount);
                break;
            default:
                throw new BadRequestException($"Order type '{pagination.Order}' not found");
        }
        
        return new(part1.Count(), part1.Skip((pagination.Page - 1) * pagination.PerPage).Take(pagination.PerPage).ToList());
    }
}