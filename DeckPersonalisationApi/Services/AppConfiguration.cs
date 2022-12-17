using DeckPersonalisationApi.Extensions;

namespace DeckPersonalisationApi.Services;

public class AppConfiguration
{
    private IConfiguration _config;
    
    public string ClientId { get; private set; }
    public string ClientSecret { get; private set; }
    public string DbPath { get; private set; }
    
    public string JwtIssuer { get; private set; }
    public string JwtAudience { get; private set; }
    public string JwtKey { get; private set; }
    public bool JwtValidateIssuer { get; private set; }
    public bool JwtValidateAudience { get; private set; }

    public string BlobPath { get; private set; }
    public string TempBlobPath { get; private set; }
    public long MaxUnconfirmedBlobs { get; private set; }
    public long MaxActiveSubmissions { get; private set; }
    public long MaxImagesPerSubmission { get; private set; }
    public List<string> CssTargets { get; private set; }
    public List<string> AudioFiles { get; private set; }
    public Dictionary<string, long> ValidFileTypesAndMaxSizes { get; private set; }
    public long MaxCssThemeSize { get; private set; }
    public long MaxAudioPackSize { get; private set; }
    public long OwnerDiscordId { get; private set; }
    public string VnuPath { get; private set; }
    public string CssToThemeJson { get; private set; }
    public long BlobTtlMinutes { get; private set; }
    public long BackgroundServiceFrequencyMinutes { get; private set; }
    public bool UseSwagger { get; private set; }
    public long Port { get; private set; }
    public string LegacyUrlBase { get; private set; }
    public string DiscordWebhook { get; private set; }
    public List<string> CorsAllowedOrigins { get; private set; }

    public AppConfiguration()
    {
        _config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.example.json", optional: true)
            .AddJsonFile($"appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        
        SetValues();
    }

    private void SetValues()
    {
        ClientId = GetString("ConnectionStrings:ClientId");
        ClientSecret = GetString("ConnectionStrings:ClientSecret");
        DbPath = GetString("ConnectionStrings:DbPath");

        JwtIssuer = GetString("Jwt:Issuer") + "/";
        JwtAudience = GetString("Jwt:Audience") + "/";
        JwtKey = GetString("Jwt:Key");
        JwtValidateIssuer = GetBool("Jwt:ValidateIssuer");
        JwtValidateAudience = GetBool("Jwt:ValidateAudience");

        BlobPath = GetString("Config:BlobPath");
        
        if (!Path.Exists(BlobPath))
            Directory.CreateDirectory(BlobPath);
        
        TempBlobPath = GetString("Config:TempBlobPath");
        
        if (!Path.Exists(TempBlobPath))
            Directory.CreateDirectory(TempBlobPath);
        
        MaxUnconfirmedBlobs = GetInt("Config:MaxUnconfirmedBlobs");
        MaxActiveSubmissions = GetInt("Config:MaxActiveSubmissions");
        MaxImagesPerSubmission = GetInt("Config:MaxImagesPerSubmission");
        CssTargets = GetList("Config:CssTargets");
        AudioFiles = GetList("Config:AudioFiles");
        
        ValidFileTypesAndMaxSizes = new Dictionary<string, long>();
        foreach (var s in GetString("Config:MaxUploadFileSizes").Split(";"))
        {
            string[] i = s.Split(":");
            if (i.Length != 2)
                throw new Exception("Failed to parse upload file sizes");
                
            ValidFileTypesAndMaxSizes.Add(i[0], long.Parse(i[1]));
        }

        MaxCssThemeSize = GetInt("Config:MaxCssThemeSize");
        MaxAudioPackSize = GetInt("Config:MaxAudioPackSize");
        OwnerDiscordId = GetInt("Config:OwnerDiscordId");
        VnuPath = GetString("Config:VnuPath");
        CssToThemeJson = GetString("Config:CssToThemeJson");
        BlobTtlMinutes = GetInt("Config:BlobTTLMinutes");
        BackgroundServiceFrequencyMinutes = GetInt("Config:BackgroundServiceFrequencyMinutes");
        UseSwagger = GetBool("Config:UseSwagger");
        Port = GetInt("Config:Port");
        LegacyUrlBase = GetString("Jwt:Audience") + (Port is 80 or 443 ? "" : ":" + Port) + "/";
        DiscordWebhook = GetString("Config:DiscordWebhook");

        CorsAllowedOrigins = GetList("Config:CorsAllowedOrigins");
    }

    private string GetString(string key)
    {
        string value = _config[key].Require($"Could not find '{key}' in configuration");
        Console.WriteLine($"Reading configuration key '{key}'" + (key.StartsWith("Config") ? $". Got value '{value}'" : ""));
        return value;
    }

    private long GetInt(string key)
        => long.Parse(GetString(key));

    private bool GetBool(string key)
        => GetString(key).ToLower() == "true";

    private List<string> GetList(string key)
        => GetString(key).Split(';').Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
}