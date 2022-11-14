using System.Text.Json.Serialization;

namespace DeckPersonalisationApi.Model.Dto;

public class DiscordUserResponse
{
    [JsonPropertyName("username")]
    public string Username { get; set; }
    
    [JsonPropertyName("discriminator")]
    public string Discriminator { get; set; }
    
    [JsonPropertyName("id")]
    public string Id { get; set; }
}