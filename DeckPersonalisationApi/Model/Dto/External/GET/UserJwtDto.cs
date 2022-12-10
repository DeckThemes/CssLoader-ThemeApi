using DeckPersonalisationApi.Exceptions;

namespace DeckPersonalisationApi.Model.Dto.External.GET;

public class UserJwtDto : IToDto<UserJwtExtDto>
{
    public string Id { get; set; }
    public string Username { get; set; }
    public Permissions Permissions { get; set; }
    public string Avatar { get; set; }
    public string ValidationToken { get; set; }

    public UserJwtDto(string id, string username, string avatar, Permissions permissions, string validationToken)
    {
        Id = id;
        Username = username;
        Permissions = permissions;
        Avatar = avatar;
        ValidationToken = validationToken;
    }

    public UserJwtDto(string id, string username, string avatar, int permissions, string validationToken) 
        : this(id, username, avatar, (Permissions)permissions, validationToken)
    {
    }

    public UserJwtDto(User user)
    {
        Id = user.Id;
        Username = user.Username;
        Permissions = user.Permissions;
        Avatar = user.GetAvatarUri()?.AbsoluteUri ?? "";
        ValidationToken = user.ValidationToken;
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