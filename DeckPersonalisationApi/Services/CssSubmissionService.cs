using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Extensions;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto.External.GET;
using DeckPersonalisationApi.Model.Dto.Internal.GET;
using Microsoft.EntityFrameworkCore;

namespace DeckPersonalisationApi.Services;

public class CssSubmissionService
{
    private ApplicationContext _ctx;
    private CssThemeService _themes;
    private BlobService _blob;
    private UserService _user;
    
    public CssSubmissionService(ApplicationContext ctx, BlobService blob, CssThemeService themes, UserService user)
    {
        _ctx = ctx;
        _blob = blob;
        _themes = themes;
        _user = user;
    }
    
    public void ApproveCssTheme(string id, string? message, User reviewer)
    {
        CssSubmission submission = GetSubmissionById(id).Require("Failed to find submission");
        CssTheme newTheme = submission.New;
        CssTheme? oldTheme = submission.Old;
        
        newTheme = _themes.GetThemeById(newTheme.Id).Require("Failed to find new theme");
        oldTheme = (oldTheme == null) ? null : _themes.GetThemeById(oldTheme.Id).Require("Failed to find old theme");

        if (oldTheme == null)
            _themes.ApproveTheme(newTheme);
        else
            _themes.ApplyThemeUpdate(oldTheme, newTheme);
        
        throw new NotImplementedException();
    }

    public void DenyCssTheme(string id, string? message, User reviewer)
    {
        CssSubmission submission = GetSubmissionById(id).Require("Failed to find submission");
        CssTheme newTheme = submission.New;
        CssTheme? oldTheme = submission.Old;

        newTheme = _themes.GetThemeById(newTheme.Id).Require("Failed to find new theme");
        oldTheme = (oldTheme == null) ? null : _themes.GetThemeById(oldTheme.Id).Require("Failed to find old theme");

        _themes.DeleteTheme(newTheme, oldTheme?.Images.All(x => newTheme.Images.Any(y => y.Id == x.Id)) ?? true, (oldTheme == null) || oldTheme.Download.Id == newTheme.Download.Id);
        
        submission.ReviewedBy = reviewer;
        submission.Status = SubmissionStatus.Denied;
        _ctx.CssSubmissions.Update(submission);
        _ctx.SaveChanges();
    }

    public CssSubmission CreateSubmission(string? oldThemeId, string newThemeId, CssSubmissionIntent intent,
        string authorId)
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
            Owner = author
        };

        _ctx.CssSubmissions.Add(submission);
        _ctx.SaveChanges();
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
            pagination.Filters.Select(x => Enum.Parse<SubmissionStatus>(x, true)).ToList();

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
}