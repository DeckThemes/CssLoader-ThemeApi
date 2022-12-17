namespace DeckPersonalisationApi.Model.Dto.External.GET;

public record UserPatchDto(bool? Active, List<string>? Permissions);