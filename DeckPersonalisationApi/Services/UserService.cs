#region

using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Extensions;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto;
using DeckPersonalisationApi.Model.Dto.External.GET;
using DeckPersonalisationApi.Model.Dto.Internal.GET;
using Microsoft.EntityFrameworkCore;

#endregion

namespace DeckPersonalisationApi.Services;

public class UserService
{
    private AppConfiguration _config;
    private ApplicationContext _ctx;
    private JwtService _jwt;

    public UserService(AppConfiguration config, ApplicationContext ctx, JwtService jwt)
    {
        _config = config;
        _ctx = ctx;
        _jwt = jwt;
    }

    public Uri BuildDiscordOauthUri(string redirectUri)
    {
        return new Uri(
            $"https://discord.com/api/oauth2/authorize?client_id={_config.ClientId}&redirect_uri={HttpUtility.UrlEncode(redirectUri)}&response_type=code&scope=identify");
    }

    public string GenerateTokenViaDiscord(string code, string redirectUri)
    {
        DiscordUserGetResponse? userResponse;
        
        using (HttpClient req = new())
        {
            Uri uri = new($"https://discordapp.com/api/oauth2/token");

            Dictionary<string, string> items = new()
            {
                {"client_id", _config.ClientId},
                {"client_secret", _config.ClientSecret},
                {"grant_type", "authorization_code"},
                {"code", code},
                {"redirect_uri", redirectUri},
            };

            var result = req.PostAsync(uri, new FormUrlEncodedContent(items)).GetAwaiter().GetResult();

            if (!result.IsSuccessStatusCode)
                throw new BadRequestException("Authentication request with discord failed");

            DiscordTokenGetResponse? token =
                result.Content.ReadFromJsonAsync<DiscordTokenGetResponse>().GetAwaiter().GetResult();

            if (token == null || token.AccessToken == null)
                throw new BadRequestException("Discord returned no information");
            
            req.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            result = req.GetAsync(new Uri("https://discordapp.com/api/users/@me")).GetAwaiter().GetResult();

            if (!result.IsSuccessStatusCode)
                throw new BadRequestException("User get request with discord failed");
            
            userResponse =
                result.Content.ReadFromJsonAsync<DiscordUserGetResponse>().GetAwaiter().GetResult();

            if (userResponse == null)
                throw new BadRequestException("Discord returned no user information");
        }

        string id = $"Discord|{userResponse.Id}";
        User? user = GetUserById(id);

        if (user == null)
        {
            user = new()
            {
                Id = id,
                Permissions = (userResponse.Id == _config.OwnerDiscordId.ToString()) ? Permissions.All : Permissions.None,
                Username = userResponse.DisplayName ?? $"{userResponse.Username}#{userResponse.Discriminator}",
                LastLoginDate = DateTimeOffset.Now,
                AvatarToken = userResponse.Avatar,
                ValidationToken = Utils.Utils.GetFixedLengthString(32)
            };

            _ctx.Users.Add(user);
            _ctx.SaveChanges();
        }
        else
        {
            if (!user.Active)
                throw new BadRequestException("Account is inactive");
            
            user.Username = userResponse.DisplayName ?? $"{userResponse.Username}#{userResponse.Discriminator}";
            user.LastLoginDate = DateTimeOffset.Now;
            user.AvatarToken = userResponse.Avatar;

            _ctx.Users.Update(user);
            _ctx.SaveChanges();
        }

        return _jwt.CreateToken(new UserJwtDto(user));
    }

    public string? GetApiToken(string userId)
    {
        User? user = GetActiveUserById(userId);
        if (user == null)
            return null;

        user.ApiToken = Utils.Utils.GetFixedLengthString(12);

        _ctx.Users.Update(user);
        _ctx.SaveChanges();
        return user.ApiToken;
    }

    public void ResetValidationToken(string userId)
    {
        User? user = GetActiveUserById(userId);
        if (user == null)
            throw new NotFoundException("User not found");

        user.ValidationToken = Utils.Utils.GetFixedLengthString(32);

        _ctx.Users.Update(user);
        _ctx.SaveChanges();
    }

    public string GenerateTokenViaApiToken(string token)
    {
        User? user = _ctx.Users.FirstOrDefault(x => x.ApiToken == token);
        if (user == null)
            throw new BadRequestException("Could not find user");
        
        if (!user.Active)
            throw new BadRequestException("Account is inactive");

        UserJwtDto jwt = new(user);
        jwt.Permissions |= Permissions.FromApiToken;
        jwt.ValidationToken = token;
        return _jwt.CreateToken(jwt);
    }
    
    public void AddStarToTheme(User user, CssTheme theme)
    {
        if (!HasThemeStarred(user, theme))
        {
            user.CssStars.Add(theme);
            _ctx.SaveChanges();
        }
    }
    
    public void RemoveStarFromTheme(User user, CssTheme theme)
    {
        if (user.CssStars.Remove(theme))
            _ctx.SaveChanges();
    }

    public User SetUserPreferences(User user, string? email, bool? active, Permissions? permissions)
    {
        user = GetUserById(user.Id).Require();
        bool change = false;

        if (email != null) // We do allow unsetting emails by having an empty string
        {
            if (email.Length > _config.MaxEmailLength)
                throw new BadRequestException($"An email can be max {_config.MaxEmailLength} characters");

            if (!(email == "" || Regex.Match(email, "^\\w+([\\.-]?\\w+)*@\\w+([\\.-]?\\w+)*(\\.\\w{2,3})+$").Success))
                throw new BadRequestException("Submitted email does not pass validation of an email");
            
            user.Email = email;
            change = true;
        }

        if (active.HasValue)
        {
            user.Active = active.Value;
            change = true;
        }

        if (permissions.HasValue)
        {
            user.Permissions = permissions.Value;
            change = true;
        }

        if (change)
        {
            _ctx.Users.Update(user);
            _ctx.SaveChanges();
        }

        return user;
    }

    public bool HasThemeStarred(User user, CssTheme theme)
        => user.CssStars.Any(x => x.Id == theme.Id);

    public User? GetUserById(string id) => _ctx.Users.Include(x => x.CssStars).FirstOrDefault(x => x.Id == id);
    public User? GetActiveUserById(string id) => _ctx.Users.Include(x => x.CssStars).FirstOrDefault(x => x.Id == id && x.Active == true);
    public List<User> GetUserByAnyPermission(Permissions permissions) => _ctx.Users.Where(x => (x.Permissions & permissions) != 0).ToList();
    public List<User> GetUsersByIds(List<string> ids) => _ctx.Users.Where(x => ids.Contains(x.Id)).ToList();
    public void UpdateBulk(List<User> updatedUsers)
    {
        if (updatedUsers.Count <= 0)
            return;
        
        foreach (var updatedUser in updatedUsers)
            _ctx.Users.Update(updatedUser);

        _ctx.SaveChanges();
    }
    public long GetSubmissionCountByUser(User user, SubmissionStatus status) => _ctx.CssSubmissions.Count(x => x.Owner == user && x.Status == status);
}