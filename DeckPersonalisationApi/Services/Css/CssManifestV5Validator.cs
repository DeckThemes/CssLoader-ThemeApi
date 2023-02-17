using DeckPersonalisationApi.Model;
using Newtonsoft.Json.Linq;

namespace DeckPersonalisationApi.Services.Css;

/*
 * V5 manifests add the ability to define css variables in place of files
 * v1.4.0 -> v1.4.1
 */
public class CssManifestV5Validator : CssManifestV4Validator
{
    public CssManifestV5Validator(string themePath, JObject json, User user, List<string> validTargets) : base(themePath, json, user, validTargets)
    {
        ManifestVersion = 5;
    }
    
    protected override void VerifySinglePatch(string key, JObject patch)
    {
        if (!patch.ContainsKey("values"))
            throw new Exception($"v{ManifestVersion} patches need to have a values key");

        if (patch["values"] is not JObject values)
            throw new Exception("Values key in patch needs to be present");

        if (patch.ContainsKey("default"))
        {
            string def = patch["default"]!.ToObject<string>()!;
            
            if (!values.ContainsKey(def))
                throw new Exception("Default key in patch needs to be present");
        }

        CheckPatchType(patch);

        List<string> options = new();

        foreach (var (option, injects) in values)
        {
            options.Add(option);
            if (injects is not JObject injectsObj)
                throw new Exception("Value of patch option is not a dictionary");
                
            foreach (var (s, jToken) in injectsObj)
            {
                if (!s.StartsWith("--")) // Essentially just skip validation on these
                    VerifySingleInject(s, jToken as JArray);
                else if (jToken is not JArray)
                    throw new Exception($"In patch option {s}, value is not an array");
            }
        }
        
        if (patch.ContainsKey("components"))
        {
            if (patch["components"] is not JArray components)
                throw new Exception("Components key needs to be an array");

            foreach (var jToken in components)
            {
                if (jToken is not JObject component)
                    throw new Exception("Component is not a dictionary");
                
                VerifyComponent(component, options);
            }
        }
    }
}