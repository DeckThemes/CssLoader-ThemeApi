namespace DeckPersonalisationApi.Model;

[Flags]
public enum Permissions
{
    None = 0x0,
    All = EditAnyPost | ApproveThemeSubmissions | ManageApi | ViewThemeSubmissions,
    EditAnyPost = 0x1, // Includes deleting any post
    ApproveThemeSubmissions = 0x2,
    ManageApi = 0x4,
    FromApiToken = 0x8,
    ViewThemeSubmissions = 0x10,
}