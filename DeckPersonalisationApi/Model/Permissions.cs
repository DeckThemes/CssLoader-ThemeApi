namespace DeckPersonalisationApi.Model;

[Flags]
public enum Permissions
{
    None = 0x0,
    EditAnyPost = 0x1, // Includes deleting any post
    ApproveThemeSubmissions = 0x2,
    ManageApi = 0x4,
}