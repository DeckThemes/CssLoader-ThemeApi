namespace DeckPersonalisationApi.Model.Dto.External.GET;

public class CssSubmissionDto
{
    public string Id { get; }
    public string Intent { get; }
    public MinimalCssThemeDto? OldTheme { get; }
    public PartialCssThemeDto NewTheme { get; }
    public string Status { get; }
    public UserGetMinimalDto? ReviewedBy { get; }
    public UserGetMinimalDto Owner { get; }
    public DateTimeOffset Submitted { get; }
    public string? Message { get; }

    public CssSubmissionDto(CssSubmission submission)
    {
        Id = submission.Id;
        Intent = submission.Intent.ToString();
        Status = submission.Status.ToString();
        
        if (submission.Old != null)
            OldTheme = ((IToDto<MinimalCssThemeDto>) submission.Old).ToDto();
        
        NewTheme = submission.New.ToDto();
        Submitted = submission.Submitted;
        Owner = submission.Owner.ToDto();
        ReviewedBy = submission.ReviewedBy?.ToDto();
        Message = submission.Message;
    }
}