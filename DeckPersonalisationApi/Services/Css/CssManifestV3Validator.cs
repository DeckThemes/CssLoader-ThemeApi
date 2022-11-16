using DeckPersonalisationApi.Model;
using Newtonsoft.Json.Linq;

namespace DeckPersonalisationApi.Services.Css;

/*
 * V3 manifests added a 'none' patch type
 * V3 also added components, specifically a 'color-picker'
 * V3 also added dependencies
 * V3 also does not require a 'default' key to be present in a patch
 * v1.2.0 -> v1.2.1
 */
public class CssManifestV3Validator : CssManifestV2Validator
{
    protected List<string> _validComponentTypes = new()
    {
        "color-picker"
    };
    
    public CssManifestV3Validator(string themePath, JObject json, User user, List<string> validTargets) : base(themePath, json, user, validTargets)
    {
        ManifestVersion = 3;
        _validPatchTypes.Add("none");
    }

    protected override void VerifyDependencies()
    {
        if (!_json.ContainsKey("dependencies"))
            return;

        if (_json["dependencies"] is not JObject dependencies)
            throw new Exception("Dependencies key is not of type list");
        
        foreach (var (key, value) in dependencies)
        {
            Dependencies.Add(key);
        }
    }

    protected virtual void VerifyComponent(JObject component, List<string> options)
    {
        List<string> requiredFields = new()
        {
            "name",
            "type",
            "on",
            "default",
            "css_variable",
            "tabs"
        };

        if (requiredFields.Any(x => !component.ContainsKey(x)))
            throw new Exception("Missing fields in component");

        string type = component["type"]!.ToObject<string>()!;
        string on = component["on"]!.ToObject<string>()!;

        if (!_validComponentTypes.Contains(type))
            throw new Exception($"Component of type '{type}' is not valid in a v{ManifestVersion} manifest");

        if (!options.Contains(on))
            throw new Exception($"Component on '{on}' value was not found in patch");
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
                VerifySingleInject(s, jToken as JArray);
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