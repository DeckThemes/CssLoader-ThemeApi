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

public static class PermissionExt
{
    public static List<string> ToList(this Permissions permissions)
    {
        List<string> perms = new();
        
        if (permissions.HasPermission(Permissions.EditAnyPost))
            perms.Add(Permissions.EditAnyPost.ToString());
        
        if (permissions.HasPermission(Permissions.ApproveThemeSubmissions))
            perms.Add(Permissions.ApproveThemeSubmissions.ToString());
        
        if (permissions.HasPermission(Permissions.ViewThemeSubmissions))
            perms.Add(Permissions.ViewThemeSubmissions.ToString());
        
        if (permissions.HasPermission(Permissions.ManageApi))
            perms.Add(Permissions.ManageApi.ToString());

        return perms;
    }

    public static Permissions FromList(List<string> items)
    {
        Permissions permissions = Permissions.None;
        foreach (var item in items)
        {
            Permissions temp;
            if (Enum.TryParse(item, out temp))
                permissions |= temp;
        }

        return permissions;
    }

    public static bool HasPermission(this Permissions permissions, Permissions has)
        => ((permissions & has) == has);
}