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

    public CssSubmissionService(CssThemeService themes, ApplicationContext ctx)
    {
        _themes = themes;
        _ctx = ctx;
    }
    
    public void ApproveCssTheme(string id, string? message)
    {
        
    }

    public void DenyCssTheme(string id, string? message)
    {
        
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