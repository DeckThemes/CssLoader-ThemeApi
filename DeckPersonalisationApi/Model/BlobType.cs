namespace DeckPersonalisationApi.Model;

public enum BlobType
{
    None,
    Jpg,
    Zip,
    Png,
}

public static class BlobTypeEx
{
    public static string GetExtension(this BlobType type)
    {
        switch (type)
        {
            case BlobType.Jpg:
                return "jpg";
            case BlobType.Png:
                return "png";
            case BlobType.Zip:
                return "zip";
            default:
                throw new NotImplementedException();
        }
    }
    
    public static string GetContentType(this BlobType type)
    {
        switch (type)
        {
            case BlobType.Jpg:
                return "image/jpg";
            case BlobType.Png:
                return "image/png";
            case BlobType.Zip:
                return "application/zip";
            default:
                throw new NotImplementedException();
        }
    }

    public static BlobType FromExtensionToBlobType(string extension)
    {
        switch (extension.ToLower())
        {
            case "jpg":
                return BlobType.Jpg;
            case "png":
                return BlobType.Png;
            case "zip":
                return BlobType.Zip;
            default:
                throw new NotImplementedException();
        }
    }
}