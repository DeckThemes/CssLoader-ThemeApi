using DeckPersonalisationApi.Model;

namespace DeckPersonalisationApi.Middleware.JwtRole;

public class JwtRoleReject : Attribute
{
    public Permissions Reject { get; set; }

    public bool OnlyIfNotPremium { get; set; }
    
    public JwtRoleReject(Permissions reject)
    {
        Reject = reject;
    }

    public JwtRoleReject(Permissions reject, bool onlyIfNotPremium)
    {
        Reject = reject;
        OnlyIfNotPremium = onlyIfNotPremium;
    }
}