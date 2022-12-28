using DeckPersonalisationApi.Model;
using Newtonsoft.Json.Linq;

namespace DeckPersonalisationApi.Services.Css;

/*
 * V4 manifests add an 'image-picker' component
 * v1.3.0 -> v1.3.2
 */
public class CssManifestV4Validator : CssManifestV3Validator
{
    public CssManifestV4Validator(string themePath, JObject json, User user, List<string> validTargets) : base(themePath, json, user, validTargets)
    {
        ManifestVersion = 4;
        _validComponentTypes.Add("image-picker");
    }
}