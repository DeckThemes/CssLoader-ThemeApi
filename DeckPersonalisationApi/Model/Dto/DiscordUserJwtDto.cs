namespace DeckPersonalisationApi.Model.Dto;

public class DiscordUserJwtDto
{
    public string Id { get; set; }
    public string Username { get; set; }
    public Permissions Permissions { get; set; }

    public DiscordUserJwtDto(string id, string username, Permissions permissions)
    {
        Id = id;
        Username = username;
        Permissions = permissions;
    }

    public DiscordUserJwtDto(string id, string username, int permissions) 
        : this(id, username, (Permissions)permissions)
    {
    }

    public DiscordUserJwtDto(User user)
    {
        Id = user.Id;
        Username = user.Username;
        Permissions = user.Permissions;
    }
}