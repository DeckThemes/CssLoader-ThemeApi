using DeckPersonalisationApi.Model;
using Newtonsoft.Json.Linq;

namespace DeckPersonalisationApi.Services.Css;

public class CssManifestV9Validator : CssManifestV7Validator
{
    public CssManifestV9Validator(string themePath, JObject json, User user, List<string> validTargets) : base(themePath, json, user, validTargets)
    {
        ManifestVersion = 9;
    }
    
    protected override void VerifyName()
    {
        if (!_json.ContainsKey("name"))
            throw new Exception("No name was found");

        Name = _json["name"]!.ToObject<string>()!;
        
        if (Name.Contains('/'))
            throw new Exception("Illegal character in theme name");
        
        if (_json.ContainsKey("display_name"))
            DisplayName = _json["display_name"]!.ToObject<string>()!;
    }
    
    protected override void VerifyTarget()
    {
        if (_json.ContainsKey("target"))
        {
            if (_json["target"] is JArray array)
            {
                foreach (var jToken in array)
                {
                    Targets.Add(jToken.ToObject<string>()!);
                }
            }
            else
            {
                Target = _json["target"]!.ToObject<string>()!;
            }
            
            _json.Remove("target");
            
            foreach (var x in Targets)
            {
                if (!_validTargets.Contains(x))
                    throw new Exception($"{x} is not a valid target");
            }
        }
    }
}