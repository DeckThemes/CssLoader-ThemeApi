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
    private List<DiscordApiGuildMember> _rawCache = new();
    private Dictionary<string, DiscordApiGuildMember> _cache = new();
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
            _rawCache = await response.Content.ReadFromJsonAsync<List<DiscordApiGuildMember>>() ?? new();

            Dictionary<string, DiscordApiGuildMember> cache = new();
            _rawCache.ForEach(x => cache.Add(x.User.Id, x));
            _cache = cache;

            return cache.Count;
        }
    }

    public List<DiscordApiUser> GetUsersWithRoles(string roleId)
        => _rawCache.Where(x => x.RoleIds.Contains(roleId)).Select(x => x.User).ToList();

    public string PermissionStateOfUser(string userId)
    {
        if (userId.StartsWith("Discord|"))
            userId = userId[8..];

        DiscordApiGuildMember? member = null;

        if (_cache.ContainsKey(userId))
            member = _cache[userId];

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