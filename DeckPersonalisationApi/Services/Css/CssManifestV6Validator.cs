using DeckPersonalisationApi.Model;
using Newtonsoft.Json.Linq;

namespace DeckPersonalisationApi.Services.Css;

/*
 * V6 manifests adds the ability to define flags for a specific theme
 * v1.6.0 -> Present
 */
public class CssManifestV6Validator : CssManifestV5Validator
{ 
    public CssManifestV6Validator(string themePath, JObject json, User user, List<string> validTargets) : base(themePath, json, user, validTargets)
    {
        ManifestVersion = 6;
    }

    protected override void VerifyFlags()
    {
        if (!_json.ContainsKey("flags"))
            return;

        if (_json["flags"] is not JArray flags)
            throw new Exception("Flags is not an array");

        Flags = flags.Select(x => x.ToObject<string>()!).Select(x =>
        {
            CssFlag? possibleFlag = null;

            if (Enum.TryParse(x, true, out CssFlag flag))
                possibleFlag = flag;

            return possibleFlag;
        }).Where(x => x.HasValue).Select(x => x.Value!).ToList();
    }
}