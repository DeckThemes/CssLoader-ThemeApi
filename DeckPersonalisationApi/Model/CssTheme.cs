namespace DeckPersonalisationApi.Model;

public class CssTheme
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<string> Images { get; set; }
    public string Version { get; set; }
    public string? Source { get; set; }
    public User Author { get; set; }
    public DateTimeOffset Submitted { get; set; }
    public DateTimeOffset Updated { get; set; }
    public string Target { get; set; }
    public int ManifestVersion { get; set; }
    public string Description { get; set; }
}