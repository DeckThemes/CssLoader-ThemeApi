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
    public static DiscordBot Instance { get; private set; }

    public DiscordBot(AppConfiguration config)
    {
        _configuration = config;
        Instance = this;
    }

    public async Task<long> Cache()
    {
        using (HttpClient client = new())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bot {_configuration.BotToken}");
            var response = await client.GetAsync($"https://discord.com/api/v10/guilds/{_configuration.DiscordServerId.ToString()}/members?limit=1000");
            // TODO: make sure this doesn't fail
            _cache = await response.Content.ReadFromJsonAsync<List<DiscordApiGuildMember>>() ?? new();
            return _cache.Count;
        }
    }

    public List<DiscordApiUser> GetUsersWithRoles(string roleId)
        => _cache.Where(x => x.RoleIds.Contains(roleId)).Select(x => x.User).ToList();

    public string PermissionStateOfUser(string userId)
    {
        if (userId.StartsWith("Discord|"))
            userId = userId[8..];

        DiscordApiGuildMember? member = _cache.Find(x => x.User.Id == userId);

        if (member != null)
        {
            if (member.RoleIds.Contains(_configuration.DiscordPremiumTier3Role.ToString()))
                return "Tier3";
            
            if (member.RoleIds.Contains(_configuration.DiscordPremiumTier2Role.ToString()))
                return "Tier2";
            
            if (member.RoleIds.Contains(_configuration.DiscordPremiumTier1Role.ToString()))
                return "Tier1";
        }

        return "None";
    }
}