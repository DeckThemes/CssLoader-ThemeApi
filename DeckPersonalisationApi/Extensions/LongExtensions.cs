namespace DeckPersonalisationApi.Extensions;

public static class LongExtensions
{
    public static string GetReadableFileSize(this long l)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = l;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1) {
            order++;
            len = len/1024;
        }

        return $"{len:0.0} {sizes[order]}";
    }
}