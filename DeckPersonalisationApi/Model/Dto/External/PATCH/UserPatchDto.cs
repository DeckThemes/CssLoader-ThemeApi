namespace DeckPersonalisationApi.Model.Dto.External.PATCH;

public record UserPatchDto(string? Email, bool? Active, List<string>? Permissions)
{
    public bool EditsAdminFields()
        => (Active.HasValue || Permissions != null);
}