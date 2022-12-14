namespace DeckPersonalisationApi.Model.Dto.External.POST;

public record GitSubmitPostDto(string Url, string? Commit, string Subfolder, SubmissionMeta Meta);
public record SubmissionMeta(List<string>? ImageBlobs, string? Description, string? Target);