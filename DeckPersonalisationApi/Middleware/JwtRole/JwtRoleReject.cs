using DeckPersonalisationApi.Model;

namespace DeckPersonalisationApi.Middleware.JwtRole;

public class JwtRoleReject : Attribute
{
    public Permissions Reject { get; set; }

    public JwtRoleReject(Permissions reject)
    {
        Reject = reject;
    }
}