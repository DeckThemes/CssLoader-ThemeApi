using System.Net.Http.Headers;
using System.Web;
using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto;

namespace DeckPersonalisationApi.Services;

public class UserService
{
    private IConfiguration _config;
    private ApplicationContext _ctx;
    private JwtService _jwt;
    private string ClientId => _config.GetConnectionString("ClientId")!;
    private string ClientSecret => _config.GetConnectionString("ClientSecret")!;

    public UserService(IConfiguration config, ApplicationContext ctx, JwtService jwt)
    {
        _config = config;
        _ctx = ctx;
        _jwt = jwt;
    }

    public Uri BuildDiscordOauthUri(string redirectUri)
    {
        return new Uri(
            $"https://discord.com/api/oauth2/authorize?client_id={ClientId}&redirect_uri={HttpUtility.UrlEncode(redirectUri)}&response_type=code&scope=identify");
    }

    public string GenerateTokenViaDiscord(string code, string redirectUri)
    {
        DiscordUserResponse? userResponse;
        
        using (HttpClient req = new())
        {
            Uri uri = new($"https://discordapp.com/api/oauth2/token");

            Dictionary<string, string> items = new()
            {
                {"client_id", ClientId},
                {"client_secret", ClientSecret},
                {"grant_type", "authorization_code"},
                {"code", code},
                {"redirect_uri", redirectUri},
            };

            var result = req.PostAsync(uri, new FormUrlEncodedContent(items)).GetAwaiter().GetResult();

            if (!result.IsSuccessStatusCode)
                throw new BadRequestException("Authentication request with discord failed");

            DiscordTokenResponse? token =
                result.Content.ReadFromJsonAsync<DiscordTokenResponse>().GetAwaiter().GetResult();

            if (token == null || token.AccessToken == null)
                throw new BadRequestException("Discord returned no information");
            
            req.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            result = req.GetAsync(new Uri("https://discordapp.com/api/users/@me")).GetAwaiter().GetResult();

            if (!result.IsSuccessStatusCode)
                throw new BadRequestException("User get request with discord failed");
            
            userResponse =
                result.Content.ReadFromJsonAsync<DiscordUserResponse>().GetAwaiter().GetResult();

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
                Permissions = Permissions.None,
                Username = $"{userResponse.Username}#{userResponse.Discriminator}",
                LastLoginDate = DateTimeOffset.Now
            };

            _ctx.Users.Add(user);
            _ctx.SaveChanges();
        }
        else
        {
            if (!user.Active)
                throw new BadRequestException("Account is inactive");
            
            user.Username = $"{userResponse.Username}#{userResponse.Discriminator}";
            user.LastLoginDate = DateTimeOffset.Now;

            _ctx.Users.Update(user);
            _ctx.SaveChanges();
        }
        
        return _jwt.CreateToken(new DiscordUserJwtDto(user));
    }

    public User? GetUserById(string id) => _ctx.Users.FirstOrDefault(x => x.Id == id);
}