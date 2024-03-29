﻿using DeckPersonalisationApi.Exceptions;
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

    public ThemeService(TaskService task, ApplicationContext ctx, BlobService blob, UserService user, AppConfiguration config)
    {
        _task = task;
        _ctx = ctx;
        _blob = blob;
        _user = user;
        _config = config;
    }

    public CssTheme CreateTheme(string id, string name, List<string> imageIds, string blobId, string version,
        string? source, string authorId, List<string> targets, int manifestVersion, string description,
        List<string> dependencyNames, string specifiedAuthor, ThemeType type, string? displayName = null)
    {
        _ctx.ChangeTracker.Clear();
        
        if (GetThemeById(id) != null)
            throw new Exception("Theme already exists");
        
        User author = _user.GetActiveUserById(authorId).Require("User not found");
        List<SavedBlob> imageBlobs = _blob.GetBlobs(imageIds).ToList();
        SavedBlob blob = _blob.GetBlob(blobId).Require();
        List<CssTheme> dependencies = new();

        if (type == ThemeType.Css && dependencyNames.Count > 0)
            dependencies = GetThemesByName(dependencyNames, ThemeType.Css).ToList();

        _blob.ConfirmBlobs(imageBlobs);
        _blob.ConfirmBlob(blob);

        CssTheme newTheme = new()
        {
            Id = id,
            Name = name,
            DisplayName = displayName,
            Images = imageBlobs,
            Download = blob,
            Version = version,
            Source = source,
            Author = author,
            Submitted = DateTimeOffset.Now,
            Updated = DateTimeOffset.Now,
            SpecifiedAuthor = specifiedAuthor,
            Target = "",
            Targets = CssTheme.ToBitfieldTargets(targets, type),
            ManifestVersion = manifestVersion,
            Description = description,
            Dependencies = dependencies,
            Visibility = PostVisibility.Private,
            Type = type
        };

        _ctx.CssThemes.Add(newTheme);
        _ctx.SaveChanges();

        return newTheme;
    }

    public void ApplyThemeUpdate(string baseId, List<string> imageIds, string blobId, string version,
        string? source, string authorId, List<string> targets, int manifestVersion, string description,
        List<string> dependencyNames, string specifiedAuthor, ThemeType type, string? displayName = null)
    {
        _ctx.ChangeTracker.Clear();
        CssTheme theme = GetThemeById(baseId).Require("Failed to find original theme");
        User author = _user.GetActiveUserById(authorId).Require("User not found");
        List<SavedBlob> imageBlobs = _blob.GetBlobs(imageIds).ToList();
        SavedBlob blob = _blob.GetBlob(blobId).Require();
        List<CssTheme> dependencies = new();
        
        if (theme.Author.Id != author.Id)
            throw new BadRequestException("Cannot overlay theme from another author");
        
        _blob.ConfirmBlobs(imageBlobs);
        _blob.ConfirmBlob(blob);
        
        if (theme.Download.Id != blob.Id)
        {
            blob.DownloadCount = theme.Download.DownloadCount;
            _ctx.Blobs.Update(blob);
            _blob.DeleteBlob(theme.Download);
        }
        
        _blob.DeleteBlobs(theme.Images.Where(x => imageBlobs.All(y => y.Id != x.Id)).ToList());
        
        theme.Images = imageBlobs;
        theme.DisplayName = displayName;
        theme.Download = blob;
        theme.Version = version;
        theme.Source = source;
        theme.Updated = DateTimeOffset.Now;
        theme.SpecifiedAuthor = specifiedAuthor;
        theme.Targets = CssTheme.ToBitfieldTargets(targets, type);
        theme.ManifestVersion = manifestVersion;
        theme.Description = description;
        theme.Dependencies = dependencies;
        
        _ctx.CssThemes.Update(theme);
        _ctx.SaveChanges();
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
        original.DisplayName = overlay.DisplayName;
        original.Download = overlay.Download;
        original.Version = overlay.Version;
        original.Source = overlay.Source;
        original.Updated = overlay.Updated;
        original.SpecifiedAuthor = overlay.SpecifiedAuthor;
        original.Targets = overlay.Targets;
        original.ManifestVersion = overlay.ManifestVersion;
        original.Description = overlay.Description;
        original.Dependencies = overlay.Dependencies;
        
        _ctx.CssThemes.Update(original);
        _ctx.SaveChanges();
    }

    public void DeleteTheme(CssTheme theme, bool deleteImages = false, bool deleteDownload = false)
    {
        theme = GetThemeById(theme.Id).Require("Could not find theme");
        theme.Visibility = PostVisibility.Deleted;
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
        theme.Visibility = PostVisibility.Public;
        _ctx.CssThemes.Update(theme);
        _ctx.SaveChanges();
    }

    public void EditTheme(CssTheme theme, string? description, string? target, User? author)
    {
        if (target != null)
        {
            if (!AppConfiguration.CssTargets.Contains(target))
                throw new BadRequestException("Target is not a valid target type");
            
            theme.Targets = CssTheme.ToBitfieldTargets(target, ThemeType.Css);
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

    public CssTheme? GetThemeById(string id, bool strict = true)
    {
        IQueryable<CssTheme> db = _ctx.CssThemes
            .Include(x => x.Dependencies)
            .Include(x => x.Author)
            .Include(x => x.Download)
            .Include(x => x.Images);

        CssTheme? theme = db.FirstOrDefault(x => x.Id == id);

        if (!strict)
            theme ??= db.FirstOrDefault(x => x.Name == id && x.Visibility == PostVisibility.Public);

        return theme;
    }

    public IEnumerable<CssTheme> GetThemesByIds(List<string> ids, bool strict = true)
        => (strict)
            ? _ctx.CssThemes
                .Where(x => ids.Contains(x.Id))
                .Where(x => x.Visibility == PostVisibility.Public)
                .ToList()
            : _ctx.CssThemes
                .Where(x => ids.Contains(x.Id) || ids.Contains(x.Name))
                .Where(x => x.Visibility == PostVisibility.Public)
                .ToList();

    public bool ThemeNameExists(string name, ThemeType type)
        => _ctx.CssThemes.Any(x => x.Name == name && x.Visibility == PostVisibility.Public && x.Type == type);

    public int GetThemeCountOfUser(User user)
        => _ctx.CssThemes.Count(x => x.Author.Id == user.Id && x.Visibility != PostVisibility.Deleted);

    public List<LegacyThemesDto> GetThemesLegacy(ThemeType type, PostVisibility visibility)
        => _ctx.CssThemes
            .Include(x => x.Images)
            .Include(x => x.Download)
            .Where(x => x.Type == type && x.Visibility == visibility)
            .ToList()
            .Select(x => new LegacyThemesDto(x, _config))
            .ToList();
    
    public IEnumerable<CssTheme> GetThemesByName(List<string> names, ThemeType type)
        => _ctx.CssThemes.Include(x => x.Author)
            .Where(x => x.Type == type).Where(x => names.Contains(x.Name) && x.Visibility == PostVisibility.Public).ToList();
    
    public IEnumerable<CssTheme> GetAnyThemesByAuthorWithName(User user, string name, ThemeType type)
        => _ctx.CssThemes.Include(x => x.Author).Include(x => x.Images).Include(x => x.Download)
            .Where(x => x.Type == type).Where(x => x.Name == name && x.Author.Id == user.Id && x.Visibility != PostVisibility.Deleted).ToList();

    public PaginatedResponse<CssTheme> GetUsersThemes(User user, PaginationDto pagination, PostVisibility visibility = PostVisibility.Public)
        => GetThemesInternal(pagination, x => x.Where(y => y.Author == user), visibility);

    public PaginatedResponse<CssTheme> GetApprovedThemes(PaginationDto pagination)
        => GetThemesInternal(pagination, null, PostVisibility.Public);
    
    public PaginatedResponse<CssTheme> GetNonApprovedThemes(PaginationDto pagination)
        => GetThemesInternal(pagination,null, PostVisibility.Private);

    public PaginatedResponse<CssTheme> GetStarredThemesByUser(PaginationDto pagination, User user)
        => GetThemesInternal(pagination, x => x.Where(y => user.CssStars.Contains(y)), null);

    public void UpdateStars()
    {
        IEnumerable<Tuple<CssTheme, long>> themes = _ctx.CssThemes
            .Include(x => x.UserStars)
            .Where(x => x.Visibility == PostVisibility.Public)
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

    public Dictionary<string, long> FiltersWithCount(string? type, User? user, bool stars = false, PostVisibility visibility = PostVisibility.Public)
    {
        IQueryable<CssTheme> part1 = _ctx.CssThemes
            .Include(x => x.Author)
            .Where(x => x.Visibility == visibility);
        
        Dictionary<string, long> filters = new();
        type = type?.ToLower() ?? null;
        IEnumerable<string> availableFilters;
        long desktopBitfieldTargets = CssTheme.ToBitfieldTargets(
            AppConfiguration.CssTargets.Where(x => x.Contains("Desktop")).ToList(), ThemeType.Css);

        switch (type)
        {
            case "css":
                part1 = part1.Where(x => x.Type == ThemeType.Css);
                availableFilters = AppConfiguration.CssTargets;
                break;
            
            case "audio":
                part1 = part1.Where(x => x.Type == ThemeType.Audio);
                availableFilters = AppConfiguration.AudioTargets;
                break;
            
            case "bpm-css":
                part1 = part1.Where(x => x.Type == ThemeType.Css && (x.Targets & desktopBitfieldTargets) == 0);
                availableFilters = AppConfiguration.CssTargets.Where(x => !x.Contains("Desktop"));
                break;
            
            case "desktop-css":
                part1 = part1.Where(x => x.Type == ThemeType.Css && (x.Targets & desktopBitfieldTargets) != 0);
                availableFilters = AppConfiguration.CssTargets.Where(x => x.Contains("Desktop"));
                break;
            
            default:
                availableFilters = AppConfiguration.CssTargets.Concat(AppConfiguration.AudioTargets);
                break;
        }
        
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

        foreach (var cssTheme in part1.ToList())
        {
            cssTheme.ToReadableTargets().ForEach(x =>
            {
                if (filters.ContainsKey(x))
                    filters[x]++;
                else
                    filters[x] = 1;
            });
        }

        filters = filters.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

        availableFilters.ToList().ForEach(x =>
        {
            if (!filters.ContainsKey(x))
                filters.Add(x, 0);
        });

        return filters;
    }

    private PaginatedResponse<CssTheme> GetThemesInternal(PaginationDto pagination, Func<IQueryable<CssTheme>, IQueryable<CssTheme>>? middleware = null, PostVisibility? status = PostVisibility.Public)
    {
        IQueryable<CssTheme> part1 = _ctx.CssThemes
            .Include(x => x.Author)
            .Include(x => x.Download)
            .Include(x => x.Images);

        if (status.HasValue)
        {
            PostVisibility actualVisibility = status.Value;
            part1 = part1.Where(x => x.Visibility == actualVisibility);
        }
        else
        {
            part1 = part1.Where(x => x.Visibility != PostVisibility.Deleted);
        }

        if (middleware != null)
            part1 = middleware(part1);
        
        long desktopBitfieldTargets = CssTheme.ToBitfieldTargets(
            AppConfiguration.CssTargets.Where(x => x.Contains("Desktop")).ToList(), ThemeType.Css);
        
        if (pagination.Filters.Contains("CSS"))
            part1 = part1.Where(x => x.Type == ThemeType.Css);
        else if (pagination.Filters.Contains("AUDIO"))
            part1 = part1.Where(x => x.Type == ThemeType.Audio);
        else if (pagination.Filters.Contains("BPM-CSS"))
            part1 = part1.Where(x => x.Type == ThemeType.Css && (x.Targets & desktopBitfieldTargets) == 0);
        else if (pagination.Filters.Contains("DESKTOP-CSS"))
            part1 = part1.Where(x => x.Type == ThemeType.Css && (x.Targets & desktopBitfieldTargets) != 0);

        pagination.Filters.RemoveAll(x => new List<string>()
        {
            "CSS",
            "AUDIO",
            "BPM-CSS",
            "DESKTOP-CSS"
        }.Contains(x));

        List<string> allTargets = AppConfiguration.CssTargets.Concat(AppConfiguration.AudioTargets).ToList();
        long filters = CssTheme.ToBitfieldTargets(pagination.Filters.Select(x => allTargets.Find(y => string.Equals(y, x, StringComparison.CurrentCultureIgnoreCase))).Where(x => x != null).ToList()!);
        long negativeFilters = CssTheme.ToBitfieldTargets(pagination.NegativeFilters.Select(x => allTargets.Find(y => string.Equals(y, x, StringComparison.CurrentCultureIgnoreCase))).Where(x => x != null).ToList()!);
        
        if (filters != 0)
            part1 = part1.Where(x => (x.Targets & filters) != 0);
        else if (negativeFilters != 0)
            part1 = part1.Where(x => (x.Targets & negativeFilters) == 0);

        if (!string.IsNullOrWhiteSpace(pagination.Search))
            part1 = part1.Where(x => (x.Name.ToLower().Contains(pagination.Search) || x.SpecifiedAuthor.ToLower().Contains(pagination.Search) || (x.DisplayName != null && x.DisplayName.ToLower().Contains(pagination.Search))));
        
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
                part1 = part1.OrderByDescending(x => x.Updated.ToString());
                break;
            case "First Updated":
                part1 = part1.OrderBy(x => x.Updated.ToString());
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