namespace DeckPersonalisationApi.Model.Dto.External.POST;

public record CssThemeGitSubmitPostDto(string Url, string? Commit, string Subfolder, CssSubmissionMeta Meta);
public record CssSubmissionMeta(List<string>? ImageBlobs, string? Description, string? Target);