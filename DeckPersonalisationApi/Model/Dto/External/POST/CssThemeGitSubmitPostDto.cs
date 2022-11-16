namespace DeckPersonalisationApi.Model.Dto.External.POST;

public record CssThemeGitSubmitPostDto(string Url, string? Commit, string Subfolder, string UserId);