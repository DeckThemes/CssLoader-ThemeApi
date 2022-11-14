namespace DeckPersonalisationApi.Model.Dto;

public class UserDto
{
    public string Id { get; set; }
    public string Username { get; set; } = "";
    public Permissions Permissions { get; set; }
    public DateTimeOffset LastLoginDate { get; set; }
    public string? SteamId { get; set; }
    public bool Active { get; set; }

    public UserDto(User user)
    {
        Id = user.Id;
        Username = user.Username;
        Permissions = user.Permissions;
        LastLoginDate = user.LastLoginDate;
        SteamId = user.SteamId;
        Active = user.Active;
    }
}