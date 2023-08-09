namespace DeckPersonalisationApi.Model.Dto.External.GET;

public class FullCssThemeDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public List<SavedBlobDto> Images { get; set; }
    public SavedBlobDto Download { get; set; }
    public string Version { get; set; }
    public string? Source { get; set; }
    public UserGetMinimalDto Author { get; set; }
    public string SpecifiedAuthor { get; set; }
    public DateTimeOffset Submitted { get; set; }
    public DateTimeOffset Updated { get; set; }
    public string Target { get; set; }
    public List<string> Targets { get; set; }
    public int ManifestVersion { get; set; }
    public string Description { get; set; }
    public List<MinimalCssThemeDto> Dependencies { get; set; }
    public bool Approved { get; set; }
    public bool Disabled { get; set; }
    public string Visibility { get; set; }
    public long StarCount { get; set; }

    public FullCssThemeDto()
    {
    }

    public FullCssThemeDto(CssTheme theme)
    {
        Id = theme.Id;
        Name = theme.Name;
        Images = theme.Images.Select(x => x.ToDto()).ToList();
        Download = theme.Download.ToDto();
        Version = theme.Version;
        Source = theme.Source;
        Author = theme.Author.ToDto();
        Submitted = theme.Submitted;
        Updated = theme.Updated;
        Targets = theme.ToReadableTargets();
        Target = Targets.First();
        ManifestVersion = theme.ManifestVersion;
        Description = theme.Description;
        Dependencies = theme.Dependencies.Select(x => ((IToDto<MinimalCssThemeDto>)x).ToDto()).ToList();
        Visibility = theme.Visibility.ToString();
        Approved = theme.Visibility == PostVisibility.Public;
        Disabled = theme.Visibility == PostVisibility.Deleted;
        SpecifiedAuthor = theme.SpecifiedAuthor;
        StarCount = theme.StarCount;
        Type = theme.Type.ToString();
    }
}