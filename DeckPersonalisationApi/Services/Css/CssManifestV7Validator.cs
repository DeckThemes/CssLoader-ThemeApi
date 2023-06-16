using DeckPersonalisationApi.Model;
using Newtonsoft.Json.Linq;

namespace DeckPersonalisationApi.Services.Css;

/*
 * V7 has a rewritten injector and allows for tab mappings
 * v1.7.0 -> Present
 */
public class CssManifestV7Validator : CssManifestV6Validator
{
    public CssManifestV7Validator(string themePath, JObject json, User user, List<string> validTargets) : base(themePath, json, user, validTargets)
    {
        ManifestVersion = 7;
    }

    protected override void VerifyTabMappings()
    {
        if (!_json.ContainsKey("tabs"))
            return;
        
        if (_json["tabs"] is not JObject tabs)
            throw new Exception($"Tab Mappings need to be a dictionary");

        foreach (var x in tabs)
        {
            if (x.Value is not JArray targetTabs)
                throw new Exception("Tab mappings element needs to be an array");
        }
    }
}