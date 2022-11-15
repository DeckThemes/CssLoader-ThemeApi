namespace DeckPersonalisationApi.Model.Dto.External.POST;

public record DiscordAuthenticatePostDto(string Code, string RedirectUrl);