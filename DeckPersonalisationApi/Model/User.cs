namespace DeckPersonalisationApi.Model;

public class User
{
    public string Id { get; set; }
    public string Username { get; set; } = "";
    public Permissions Permissions { get; set; }
    public DateTimeOffset LastLoginDate { get; set; }
    public string? SteamId { get; set; }
    public bool Active { get; set; } = true;
}