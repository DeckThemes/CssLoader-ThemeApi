namespace DeckPersonalisationApi.Model.Dto.External.GET;

public class SavedBlobDto
{
    public string Id { get; }
    public string BlobType { get; }
    public DateTimeOffset Uploaded { get; }
    public long DownloadCount { get;  }

    public SavedBlobDto(SavedBlob blob)
    {
        Id = blob.Id;
        BlobType = blob.Type.ToString();
        Uploaded = blob.Uploaded;
        DownloadCount = blob.DownloadCount;
    }
}