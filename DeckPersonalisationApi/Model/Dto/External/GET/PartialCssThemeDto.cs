namespace DeckPersonalisationApi.Model.Dto.External.GET;

public class PartialCssThemeDto
{
    public string Id { get; }
    public string Name { get; }
    public List<SavedBlobDto> Images { get; }
    public SavedBlobDto Download { get; }
    public string Version { get; }
    public string Target { get; }
    public int ManifestVersion { get; }

    public UserGetMinimalDto Author { get; }
    public string SpecifiedAuthor { get; }
    public DateTimeOffset Submitted { get; }
    public DateTimeOffset Updated { get; }

    public PartialCssThemeDto(CssTheme theme)
    {
        Id = theme.Id;
        Name = theme.Name;
        Version = theme.Version;
        Target = theme.Target;
        ManifestVersion = theme.ManifestVersion;
        Images = theme.Images.Select(x => x.ToDto()).ToList();
        Download = theme.Download.ToDto();
        Author = theme.Author.ToDto();
        Submitted = theme.Submitted;
        Updated = theme.Updated;
        SpecifiedAuthor = theme.SpecifiedAuthor;
    }
}