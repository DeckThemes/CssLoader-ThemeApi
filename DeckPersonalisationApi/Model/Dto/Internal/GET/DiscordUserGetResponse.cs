#region

using System.Text.Json.Serialization;

#endregion

namespace DeckPersonalisationApi.Model.Dto.Internal.GET;

public class DiscordUserGetResponse
{
    [JsonPropertyName("username")]
    public string Username { get; set; }
    
    [JsonPropertyName("discriminator")]
    public string Discriminator { get; set; }
    
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }
}