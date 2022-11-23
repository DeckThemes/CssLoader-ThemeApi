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
    private BlobService _blob;
    
    public CssSubmissionService(ApplicationContext ctx, BlobService blob)
    {
        _ctx = ctx;
        _blob = blob;
    }
    
    public void ApproveCssTheme(string id, string? message, User reviewer)
    {
        /*
        CssSubmission submission = GetSubmissionById(id).Require("Failed to find submission");
        CssTheme baseTheme = _themes.GetThemeById(submission.Theme.Id).Require("Failed to find base theme");
        CssTheme? updateTheme = submission.ThemeUpdate != null ? _themes.GetThemeById(submission.ThemeUpdate.Id) : null;

        if (updateTheme == null && submission.Intent == CssSubmissionIntent.UpdateTheme)
            throw new NotFoundException("Failed to find update theme patch");
        
        if (submission.Intent == CssSubmissionIntent.NewTheme)
        {
            _themes.ApproveTheme(baseTheme);
        }
        else if (submission.Intent == CssSubmissionIntent.UpdateMeta)
        {
            if (submission.TargetChange != null)
                baseTheme.Target = submission.TargetChange;

            if (submission.DescriptionChange != null)
                baseTheme.Description = submission.DescriptionChange;

            if (submission.ImagesChange != null)
                baseTheme.Images = submission.ImagesChange;

            _ctx.CssThemes.Update(baseTheme);
            _ctx.SaveChanges();
        }
        else if (submission.Intent == CssSubmissionIntent.UpdateTheme)
        {
            throw new NotImplementedException();
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
        */
        throw new NotImplementedException();
    }

    public void DenyCssTheme(string id, string? message, User reviewer)
    {
        /*
        CssSubmission submission = GetSubmissionById(id).Require("Failed to find submission");
        CssTheme baseTheme = _themes.GetThemeById(submission.Theme.Id).Require("Failed to find base theme");
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
        */
        throw new NotImplementedException();
    }

    public CssSubmission CreateSubmission(CssTheme? oldTheme, CssTheme newTheme, CssSubmissionIntent intent,
        User author)
    {
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
            .Include(x => x.ImagesChange)
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
            .Include(x => x.ImagesChange)
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