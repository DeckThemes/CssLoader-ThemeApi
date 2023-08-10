namespace DeckPersonalisationApi.Model.Dto.External.GET;

public class MinimalCssThemeDto
{
    public string Id { get; }
    public string Name { get; }
    public string DisplayName { get; }
    public string Type { get; }
    public string Version { get; }
    public string Target { get; }
    public List<string> Targets { get; }
    public string SpecifiedAuthor { get; }
    public int ManifestVersion { get; }

    public MinimalCssThemeDto(CssTheme theme)
    {
        Id = theme.Id;
        Name = theme.Name;
        DisplayName = theme.DisplayName ?? Name;
        Version = theme.Version;
        Targets = theme.ToReadableTargets();
        Target = Targets.First();
        ManifestVersion = theme.ManifestVersion;
        SpecifiedAuthor = theme.SpecifiedAuthor;
        Type = theme.Type.ToString();
    }
}