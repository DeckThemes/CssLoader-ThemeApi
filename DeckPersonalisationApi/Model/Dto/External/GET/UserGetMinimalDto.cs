namespace DeckPersonalisationApi.Model.Dto.External.GET;

public class UserGetMinimalDto
{
    public string Id { get; set; }
    public string Username { get; set; }
    public Uri? Avatar { get; set; }
    public bool Active { get; set; }

    public List<string> Permissions { get; set; } = new();

    public UserGetMinimalDto(User user)
    {
        Id = user.Id;
        Username = user.Username;
        Avatar = user.GetAvatarUri();
        Active = user.Active;
        Permissions = user.Permissions.ToList();
    }
}