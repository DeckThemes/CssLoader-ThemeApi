using DeckPersonalisationApi.Exceptions;

namespace DeckPersonalisationApi.Model.Dto.External.GET;

public class UserJwtDto : IToDto<UserJwtExtDto>
{
    public string Id { get; set; }
    public string Username { get; set; }
    public Permissions Permissions { get; set; }
    public string Avatar { get; set; }

    public UserJwtDto(string id, string username, string avatar, Permissions permissions)
    {
        Id = id;
        Username = username;
        Permissions = permissions;
        Avatar = avatar;
    }

    public UserJwtDto(string id, string username, string avatar, int permissions) 
        : this(id, username, avatar, (Permissions)permissions)
    {
    }

    public UserJwtDto(User user)
    {
        Id = user.Id;
        Username = user.Username;
        Permissions = user.Permissions;
        Avatar = user.GetAvatarUri()?.AbsoluteUri ?? "";
    }

    public void RequirePermission(Permissions permission)
    {
        if ((Permissions & permission) != permission)
            throw new UnauthorisedException("Token is not allowed to do this action");
    }

    public void RejectPermission(Permissions permission)
    {
        if ((Permissions & permission) != 0)
            throw new UnauthorisedException("Token is not allowed to do this action");
    }

    public UserJwtExtDto ToDto()
        => new(this);

    public object ToDtoObject()
        => ToDto();
}