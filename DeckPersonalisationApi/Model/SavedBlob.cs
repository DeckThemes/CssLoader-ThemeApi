namespace DeckPersonalisationApi.Model;

public class SavedBlob
{
    public string Id { get; set; }
    public User Owner { get; set; }
    public BlobType Type { get; set; }
    public bool Confirmed { get; set; } = false;
    public DateTimeOffset Uploaded { get; set; }
}