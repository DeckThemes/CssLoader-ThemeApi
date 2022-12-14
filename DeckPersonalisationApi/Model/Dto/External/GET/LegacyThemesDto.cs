using System.Text.Json.Serialization;
using DeckPersonalisationApi.Services;

namespace DeckPersonalisationApi.Model.Dto.External.GET;

public class LegacyThemesDto
{
    [JsonPropertyName("id")]
    public string Id { get; }
    [JsonPropertyName("download_url")]
    public string DownloadUrl { get; }
    [JsonPropertyName("preview_image")]
    public string PreviewImage { get; }
    [JsonPropertyName("name")]
    public string Name { get; }
    [JsonPropertyName("version")]
    public string Version { get; }
    [JsonPropertyName("author")]
    public string Author { get; }
    [JsonPropertyName("last_changed")]
    public string LastChanged { get; }
    [JsonPropertyName("target")]
    public string Target { get; }
    [JsonPropertyName("source")]
    public string Source { get; }
    [JsonPropertyName("manifest_version")]
    public int ManifestVersion { get; }
    [JsonPropertyName("description")]
    public string Description { get; }
    [JsonPropertyName("music")]
    public bool MusicPack { get; }

    public LegacyThemesDto(CssTheme theme, AppConfiguration config)
    {
        Id = theme.Id;
        DownloadUrl = config.LegacyUrlBase + "blobs/" + theme.Download.Id;
        PreviewImage = theme.Images.Count >= 1
            ? config.LegacyUrlBase + "blobs/" + theme.Images.First().Id
            : "https://cdn.discordapp.com/attachments/1013235079918653471/1051308155394592778/Untitled.png";
        Name = theme.Name;
        Version = theme.Version;
        Author = theme.SpecifiedAuthor;
        LastChanged = theme.Updated.ToString();
        Target = theme.Target;
        Source = theme.Source ?? "";
        ManifestVersion = theme.ManifestVersion;
        Description = theme.Description;
        MusicPack = theme.Target == "Music";
    }
}