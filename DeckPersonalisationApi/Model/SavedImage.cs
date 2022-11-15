namespace DeckPersonalisationApi.Model;

public class SavedImage
{
    public string Id { get; set; }
    public string Path { get; set; }
    public User Owner { get; set; }
}