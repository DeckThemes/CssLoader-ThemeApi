namespace DeckPersonalisationApi.Model.Dto.External.GET;

public class PartialCssThemeDto
{
    public string Id { get; }
    public string Name { get; }
    public string Type { get; }
    public List<SavedBlobDto> Images { get; }
    public SavedBlobDto Download { get; }
    public string Version { get; }
    public string Target { get; }
    public List<string> Targets { get; }
    public int ManifestVersion { get; }
    public UserGetMinimalDto Author { get; }
    public string SpecifiedAuthor { get; }
    public DateTimeOffset Submitted { get; }
    public DateTimeOffset Updated { get; }
    public long StarCount { get; }

    public PartialCssThemeDto(CssTheme theme)
    {
        Id = theme.Id;
        Name = theme.Name;
        Version = theme.Version;
        Targets = theme.ToReadableTargets();
        Target = Targets.First();
        ManifestVersion = theme.ManifestVersion;
        Images = theme.Images.Select(x => x.ToDto()).ToList();
        Download = theme.Download.ToDto();
        Author = theme.Author.ToDto();
        Submitted = theme.Submitted;
        Updated = theme.Updated;
        SpecifiedAuthor = theme.SpecifiedAuthor;
        StarCount = theme.StarCount;
        Type = theme.Type.ToString();
    }
}