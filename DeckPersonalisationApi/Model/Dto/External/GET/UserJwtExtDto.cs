namespace DeckPersonalisationApi.Model.Dto.External.GET;

public class UserJwtExtDto
{
    public string Id { get; set; }
    public string Username { get; set; }
    public List<string> Permissions { get; set; }
    public string Avatar { get; set; }

    public UserJwtExtDto(UserJwtDto jwt)
    {
        Id = jwt.Id;
        Username = jwt.Username;
        Permissions = jwt.Permissions.ToList();
        Avatar = jwt.Avatar;
    }
}