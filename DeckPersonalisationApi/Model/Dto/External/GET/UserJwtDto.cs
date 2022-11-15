using DeckPersonalisationApi.Exceptions;

namespace DeckPersonalisationApi.Model.Dto.External.GET;

public class UserJwtDto
{
    public string Id { get; set; }
    public string Username { get; set; }
    public Permissions Permissions { get; set; }

    public UserJwtDto(string id, string username, Permissions permissions)
    {
        Id = id;
        Username = username;
        Permissions = permissions;
    }

    public UserJwtDto(string id, string username, int permissions) 
        : this(id, username, (Permissions)permissions)
    {
    }

    public UserJwtDto(User user)
    {
        Id = user.Id;
        Username = user.Username;
        Permissions = user.Permissions;
    }

    public void RequirePermission(Permissions permission)
    {
        if ((Permissions & permission) != permission)
            throw new UnauthorisedException("User is not allowed to do this action");
    }

    public void RejectPermission(Permissions permission)
    {
        if ((Permissions & permission) != 0)
            throw new UnauthorisedException("User is not allowed to do this action");
    }
}