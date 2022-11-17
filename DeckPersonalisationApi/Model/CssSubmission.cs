namespace DeckPersonalisationApi.Model;

public class CssSubmission
{
    public string Id { get; set; }
    public CssTheme Theme { get; set; }
    public SubmissionStatus Status { get; set; }
    public string? Message { get; set; }
}