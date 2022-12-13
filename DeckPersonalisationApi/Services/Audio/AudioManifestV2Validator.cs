using DeckPersonalisationApi.Model;
using Newtonsoft.Json.Linq;

namespace DeckPersonalisationApi.Services.Audio;

public class AudioManifestV2Validator : AudioManifestV1Validator
{
    public AudioManifestV2Validator(string packPath, JObject json, User user, List<string> validFiles) : base(packPath, json, user, validFiles)
    {
        ManifestVersion = 2;
    }

    protected override void VerifyMappings()
    {
        if (!_json.ContainsKey("mappings"))
            return;
        
        if (_json["mappings"] is not JObject patches)
            throw new Exception("Patches section of pack.json is not a dictionary");
        
        foreach (var (key, value) in patches)
        {
            if (value is not JArray files)
                throw new Exception("Mappings item is not of type array");

            if (!_validFiles.Contains(key))
                throw new Exception($"{key} in mappings is not a valid sound override");
            
            Mappings.Add(key, new());

            foreach (var jToken in files)
            {
                string file = jToken.ToObject<string>()!;
                string path = Path.Join(_packPath, file);

                if (!File.Exists(path))
                    throw new Exception($"File {file} does not exist");
                
                Mappings[key].Add(file);
            }

            if (Mappings[key].Count <= 0)
                throw new Exception("Empty mapping");
        }
    }
}