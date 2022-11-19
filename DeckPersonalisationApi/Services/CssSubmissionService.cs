using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto.External.GET;
using DeckPersonalisationApi.Model.Dto.Internal.GET;
using Microsoft.EntityFrameworkCore;

namespace DeckPersonalisationApi.Services;

public class CssSubmissionService
{
    private CssThemeService _themes;
    private ApplicationContext _ctx;
    private BlobService _blob;
    
    public CssSubmissionService(CssThemeService themes, ApplicationContext ctx, BlobService blob)
    {
        _themes = themes;
        _ctx = ctx;
        _blob = blob;
    }
    
    public void ApproveCssTheme(string id, string? message, User reviewer)
    {
        CssSubmission? submission = GetSubmissionById(id);

        if (submission == null)
            throw new NotFoundException("Failed to find submission");
        
        CssTheme? baseTheme = _themes.GetThemeById(submission.Theme.Id);

        if (baseTheme == null)
            throw new NotFoundException("Failed to find base theme");
        
        CssTheme? updateTheme = submission.ThemeUpdate != null ? _themes.GetThemeById(submission.ThemeUpdate.Id) : null;

        if (updateTheme == null && submission.Intent == CssSubmissionIntent.UpdateTheme)
            throw new NotFoundException("Failed to find update theme patch");
        
        if (submission.Intent == CssSubmissionIntent.NewTheme)
        {
            _themes.ApproveTheme(baseTheme);
        }
        else if (submission.Intent == CssSubmissionIntent.UpdateMeta)
        {
            
        }
        else if (submission.Intent == CssSubmissionIntent.UpdateTheme)
        {

        }
        else
        {
            throw new Exception("Submission has unknown intent");
        }
        
        submission.Status = SubmissionStatus.Approved;
        submission.ReviewedBy = reviewer;
        submission.Message = message;
        _ctx.CssSubmissions.Update(submission);
        _ctx.SaveChanges();
    }

    public void DenyCssTheme(string id, string? message, User reviewer)
    {
        CssSubmission? submission = GetSubmissionById(id);

        if (submission == null)
            throw new NotFoundException("Failed to find submission");
        
        CssTheme? baseTheme = _themes.GetThemeById(submission.Theme.Id);

        if (baseTheme == null)
            throw new NotFoundException("Failed to find base theme");
        
        CssTheme? updateTheme = submission.ThemeUpdate != null ? _themes.GetThemeById(submission.ThemeUpdate.Id) : null;

        if (updateTheme == null && submission.Intent == CssSubmissionIntent.UpdateTheme)
            throw new NotFoundException("Failed to find update theme patch");

        if (submission.Intent == CssSubmissionIntent.NewTheme)
        {
            _blob.DeleteBlob(baseTheme.Download);
            _themes.DisableTheme(baseTheme);
        }
        else if (submission.Intent == CssSubmissionIntent.UpdateMeta)
        {
            if (submission.ImagesChange != null)
                _blob.DeleteBlobs(submission.ImagesChange);
        }
        else if (submission.Intent == CssSubmissionIntent.UpdateTheme)
        {
            _blob.DeleteBlob(updateTheme!.Download);
            _themes.DisableTheme(updateTheme);
        }
        else
        {
            throw new Exception("Submission has unknown intent");
        }

        submission.Status = SubmissionStatus.Denied;
        submission.ReviewedBy = reviewer;
        submission.Message = message;
        _ctx.CssSubmissions.Update(submission);
        _ctx.SaveChanges();
    }

    public IEnumerable<string> Orders() => new List<string>()
    {
        "Last to First",
        "First to Last",
    };

    public IEnumerable<string> Filters() => new List<string>()
    {
        CssSubmissionIntent.NewTheme.ToString(),
        CssSubmissionIntent.UpdateMeta.ToString(),
        CssSubmissionIntent.UpdateTheme.ToString()
    };

    public CssSubmission? GetSubmissionById(string id)
        => _ctx.CssSubmissions
            .Include(x => x.ImagesChange)
            .Include(x => x.ReviewedBy)
            .Include(x => x.Owner)
            .Include(x => x.Theme)
            .Include(x => x.ThemeUpdate)
            .FirstOrDefault(x => x.Id == id);
    
    public PaginatedResponse<CssSubmission> GetAwaitingApprovalSubmissions(PaginationDto pagination)
        => GetSubmissionsInternal(pagination, x => x.Where(y => y.Status == SubmissionStatus.AwaitingApproval));

    public PaginatedResponse<CssSubmission> GetSubmissionsFromUser(PaginationDto pagination, User user)
        => GetSubmissionsInternal(pagination, x => x.Where(y => y.Owner == user));

    private PaginatedResponse<CssSubmission> GetSubmissionsInternal(PaginationDto pagination, Func<IEnumerable<CssSubmission>, IEnumerable<CssSubmission>> middleware)
    {
        List<CssSubmissionIntent> intents =
            pagination.Filters.Select(x => Enum.Parse<CssSubmissionIntent>(x, true)).ToList();
        
        IEnumerable<CssSubmission> part1 = _ctx.CssSubmissions
            .Include(x => x.ImagesChange)
            .Include(x => x.ReviewedBy)
            .Include(x => x.Owner)
            .Include(x => x.Theme)
            .Include(x => x.Theme.Dependencies)
            .Include(x => x.Theme.Author)
            .Include(x => x.Theme.Download)
            .Include(x => x.Theme.Images)
            .Include(x => x.ThemeUpdate);

        part1 = middleware(part1);
        part1 = part1.Where(x => ((intents.Count <= 0) || intents.Contains(x.Intent)));
        
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