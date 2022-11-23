using DeckPersonalisationApi.Model.Dto.External.GET;

namespace DeckPersonalisationApi.Model;

public class CssSubmission : IToDto<CssSubmissionDto>
{
    public string Id { get; set; }
    public CssSubmissionIntent Intent { get; set; }
    public CssTheme? Old { get; set; }
    public CssTheme New { get; set; }
    public SubmissionStatus Status { get; set; }
    public DateTimeOffset Submitted { get; set; }
    public User? ReviewedBy { get; set; }
    public User Owner { get; set; }
    public string? Message { get; set; }
    
    public string? DescriptionChange { get; set; }
    public List<SavedBlob>? ImagesChange { get; set; }
    public string? TargetChange { get; set; }

    public CssSubmissionDto ToDto()
        => new(this);

    public object ToDtoObject()
        => ToDto();
}