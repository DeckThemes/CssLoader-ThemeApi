namespace DeckPersonalisationApi.Model;

public class User
{
    public string Id { get; set; }
    public string Username { get; set; } = "";
    public string? AvatarToken { get; set; }
    public Permissions Permissions { get; set; }
    public DateTimeOffset LastLoginDate { get; set; }
    public string? ApiToken { get; set; }
    public bool Active { get; set; } = true;

    public Uri? GetAvatarUri()
    {
        if (Id.StartsWith("Discord|"))
        {
            if (AvatarToken == null)
                return null;
            
            string userId = Id[8..];
            return new Uri($"https://cdn.discordapp.com/avatars/{userId}/{AvatarToken}");
        }

        return null;
    }
}