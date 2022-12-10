namespace DeckPersonalisationApi.Model.Dto.External.POST;

public record UserEditDto(List<string>? Permissions, bool? Enabled);