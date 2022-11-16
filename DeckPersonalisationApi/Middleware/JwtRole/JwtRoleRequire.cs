using DeckPersonalisationApi.Model;

namespace DeckPersonalisationApi.Middleware.JwtRole;

public class JwtRoleRequire : Attribute
{
    public Permissions Require { get; set; }

    public JwtRoleRequire(Permissions require)
    {
        Require = require;
    }
}