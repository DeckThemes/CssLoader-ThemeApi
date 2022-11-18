namespace DeckPersonalisationApi.Model.Dto.External.GET;

public class CssSubmissionDto
{
    public string Id { get; }
    public string Intent { get; }
    public FullCssThemeDto Theme { get; }
    public SubmissionStatus Status { get; }
    public UserGetMinimalDto? ReviewedBy { get; }
    public UserGetMinimalDto Owner { get; }
    public DateTimeOffset Submitted { get; }
    public string? Message { get; }

    public CssSubmissionDto(CssSubmission submission)
    {
        Id = submission.Id;
        Intent = submission.Intent.ToString();
        Status = submission.Status;
        Theme = ((IToDto<FullCssThemeDto>)submission.Theme).ToDto();
        Submitted = submission.Submitted;
        Owner = submission.Owner.ToDto();
        
        if (Status != SubmissionStatus.AwaitingApproval)
        {
            ReviewedBy = submission.ReviewedBy?.ToDto();
            Message = submission.Message;
            return;
        }

        if (submission.Intent == CssSubmissionIntent.UpdateMeta)
        {
            Theme.Description = submission.DescriptionChange ?? Theme.Description;
            Theme.Images = submission.ImagesChange?.Select(x => x.ToDto()).ToList() ?? Theme.Images;
            Theme.Target = submission.TargetChange ?? Theme.Target;
            return;
        }

        if (submission.Intent == CssSubmissionIntent.UpdateTheme)
        {
            CssTheme update = submission.ThemeUpdate!;
            Theme.ManifestVersion = update.ManifestVersion;
            Theme.Version = update.Version;
            Theme.Dependencies = update.Dependencies.Select(x => ((IToDto<MinimalCssThemeDto>)x).ToDto()).ToList();
            Theme.Download = update.Download.ToDto();
            Theme.Source = update.Source;
        }
    }
}