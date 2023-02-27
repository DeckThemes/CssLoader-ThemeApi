using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace DeckPersonalisationApi.Services;

public class DiscordApiUser
{
    [JsonProperty("id")] 
    [JsonPropertyName("id")]
    public string Id { get; set; }
}

public class DiscordApiGuildMember
{
    [JsonProperty("user")]
    [JsonPropertyName("user")]
    public DiscordApiUser User { get; set; }
    
    [JsonProperty("roles")]
    [JsonPropertyName("roles")]
    public List<string> RoleIds { get; set; }
}

public class DiscordBot
{
    private AppConfiguration _configuration;
    private List<DiscordApiGuildMember> _cache = new();

    public DiscordBot(AppConfiguration config)
    {
        _configuration = config;
    }

    public async Task Cache()
    {
        using (HttpClient client = new())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bot {_configuration.BotToken}");
            var response = await client.GetAsync($"https://discord.com/api/v10/guilds/{_configuration.DiscordServerId.ToString()}/members?limit=1000");
            // TODO: make sure this doesn't fail
            _cache = await response.Content.ReadFromJsonAsync<List<DiscordApiGuildMember>>() ?? new();
        }
    }

    public List<DiscordApiUser> GetUsersWithRoles(string roleId)
        => _cache.Where(x => x.RoleIds.Contains(roleId)).Select(x => x.User).ToList();
}