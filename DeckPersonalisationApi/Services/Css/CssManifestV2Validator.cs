using DeckPersonalisationApi.Model;
using Newtonsoft.Json.Linq;

namespace DeckPersonalisationApi.Services.Css;

/*
 * V2 manifests added patch types, and moved the selectable options of a patch into a 'values' key
 * v1.1.0 -> v1.1.0
 */
public class CssManifestV2Validator : CssManifestV1Validator
{
    protected List<string> _validPatchTypes = new()
    {
        "dropdown",
        "checkbox",
        "slider"
    };
    
    public CssManifestV2Validator(string themePath, JObject json, User user, List<string> validTargets) 
        : base(themePath, json, user, validTargets)
    {
        ManifestVersion = 2;
    }

    protected virtual void CheckPatchType(JObject patch)
    {
        if (patch.ContainsKey("type"))
        {
            string type = patch["type"]!.ToObject<string>()!;
            if (!_validPatchTypes.Contains(type))
                throw new Exception($"Patch of type '{type}' is not valid in a v{ManifestVersion} manifest");
        }
    }

    protected override void VerifySinglePatch(string key, JObject patch)
    {
        if (!patch.ContainsKey("default"))
            throw new Exception($"v{ManifestVersion} patches need to have a default key in a patch");

        string def = patch["default"]!.ToObject<string>()!;

        if (!patch.ContainsKey("values"))
            throw new Exception($"v{ManifestVersion} patches need to have a values key");

        if (patch["values"] is not JObject values)
            throw new Exception("Values key in patch needs to be present");

        if (!values.ContainsKey(def))
            throw new Exception("Default key in patch needs to be present");

        CheckPatchType(patch);

        foreach (var (option, injects) in values)
        {
            if (injects is not JObject injectsObj)
                throw new Exception("Value of patch option is not a dictionary");
                
            foreach (var (s, jToken) in injectsObj)
            {
                VerifySingleInject(s, jToken as JArray);
            }
        }
    }
}