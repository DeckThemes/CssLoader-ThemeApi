namespace DeckPersonalisationApi.Model;

public class CssSubmission
{
    public string Id { get; set; }
    public CssSubmissionIntent Intent { get; set; }
    public CssTheme Theme { get; set; }
    public CssTheme? ThemeUpdate { get; set; }
    public SubmissionStatus Status { get; set; }
    public string? Message { get; set; }
    
    public string? DescriptionChange { get; set; }
    public List<SavedBlob>? ImagesChange { get; set; }
    public string? TargetChange { get; set; }
}