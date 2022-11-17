namespace DeckPersonalisationApi.Model;

public class CssTheme
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<SavedBlob> Images { get; set; }
    public SavedBlob Download { get; set; }
    public string Version { get; set; }
    public string? Source { get; set; }
    public User Author { get; set; }
    public DateTimeOffset Submitted { get; set; }
    public DateTimeOffset Updated { get; set; }
    public string Target { get; set; }
    public int ManifestVersion { get; set; }
    public string Description { get; set; }
    public List<CssTheme> Dependencies { get; set; }
    public bool Approved { get; set; }
}