﻿using System.ComponentModel.DataAnnotations.Schema;
using DeckPersonalisationApi.Model.Dto.External.GET;

namespace DeckPersonalisationApi.Model;

public class User : IToDto<UserGetMinimalDto>, IToDto<UserGetDto>
{
    public string Id { get; set; }
    public string Username { get; set; } = "";
    public string? AvatarToken { get; set; }
    public Permissions Permissions { get; set; }
    public DateTimeOffset LastLoginDate { get; set; }
    public string ValidationToken { get; set; }
    public string? ApiToken { get; set; }
    public string? Email { get; set; }
    public bool Active { get; set; } = true;
    public ICollection<CssTheme> CssStars { get; set; }
    [InverseProperty("Author")]
    public ICollection<CssTheme> CssThemes { get; set; }

    public Uri? GetAvatarUri()
    {
        if (Id.StartsWith("Discord|"))
        {
            if (AvatarToken == null)
            {
                if (Username.Contains("#"))
                {
                    int discrim = int.Parse(Username.Split("#").Last().Trim());
                    return new Uri($"https://cdn.discordapp.com/embed/avatars/{discrim % 5}.png");
                }
                else
                {
                    long id = long.Parse(Id.Substring(8));
                    return new Uri($"https://cdn.discordapp.com/embed/avatars/{(id >> 22) % 6}.png");
                }
            }
            
            string userId = Id[8..];
            return new Uri($"https://cdn.discordapp.com/avatars/{userId}/{AvatarToken}");
        }

        return null;
    }

    public UserGetMinimalDto ToDto()
        => new(this);

    UserGetDto IToDto<UserGetDto>.ToDto()
        => new(this);

    public object ToDtoObject()
        => ToDto();
}