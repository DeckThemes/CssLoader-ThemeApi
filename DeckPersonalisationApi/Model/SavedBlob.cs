using DeckPersonalisationApi.Model.Dto.External.GET;

namespace DeckPersonalisationApi.Model;

public class SavedBlob : IToDto<SavedBlobDto>
{
    public string Id { get; set; }
    public User Owner { get; set; }
    public BlobType Type { get; set; }
    public bool Confirmed { get; set; } = false;
    public DateTimeOffset Uploaded { get; set; }
    public long DownloadCount { get; set; }

    public SavedBlobDto ToDto()
        => new(this);

    public object ToDtoObject()
        => ToDto();
}