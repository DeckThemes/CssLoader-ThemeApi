﻿namespace DeckPersonalisationApi.Model.Dto.External.GET;

public class UserGetDto
{
    public string Id { get; set; }
    public string Username { get; set; }
    public Uri? Avatar { get; set; }
    public List<string> Permissions { get; set; }
    public DateTimeOffset LastLoginDate { get; set; }
    public bool Active { get; set; }
    public bool HasApiKey { get; set; }

    public UserGetDto(User user)
    {
        Id = user.Id;
        Username = user.Username;
        Permissions = user.Permissions.ToList();
        LastLoginDate = user.LastLoginDate;
        Active = user.Active;
        Avatar = user.GetAvatarUri();
        HasApiKey = user.ApiToken != null;
    }
}