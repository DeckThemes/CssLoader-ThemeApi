using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Model;
using Newtonsoft.Json.Linq;

namespace DeckPersonalisationApi.Services.Css;

/*
 * Initial release
 * v0.1.1 -> v1.0.0
 */
public class CssManifestV1Validator
{
    protected string _themePath;
    protected JObject _json;
    protected List<string> _validTargets;

    public string Name { get; protected set; } = "";
    public string Author { get; protected set; }
    public string Version { get; protected set; } = "v1.0";
    public string? Target { get; protected set; }
    public int ManifestVersion { get; protected set; }
    public string? Description { get; protected set; }
    public List<string> Dependencies { get; protected set; } = new();
    public List<string> CssPaths { get; protected set; } = new();
    public List<CssFlag> Flags { get; protected set; } = new();

    public CssManifestV1Validator(string themePath, JObject json, User user, List<string> validTargets)
    {
        _themePath = themePath;
        _json = json;
        Author = user.Username;
        _validTargets = validTargets;
        ManifestVersion = 1;
    }

    protected virtual void VerifySingleInject(string key, JArray? tabs)
    {
        if (tabs == null)
            throw new Exception("Tabs in single inject object is not a list");
        
        if (!File.Exists(Path.Join(_themePath, key)))
            throw new Exception($"File {key} does not exist");

        if (!key.EndsWith(".css"))
            throw new Exception($"File {key} is not a .css file");
        
        if (!CssPaths.Contains(key))
            CssPaths.Add(key);
    }

    protected virtual void VerifyName()
    {
        if (!_json.ContainsKey("name"))
            throw new Exception("No name was found");

        Name = _json["name"]!.ToObject<string>()!;
    }

    protected virtual void VerifyAuthor()
    {
        if (_json.ContainsKey("author"))
            Author = _json["author"]!.ToObject<string>()!;
        else
            _json["author"] = Author;
    }

    protected virtual void VerifyVersion()
    {
        if (_json.ContainsKey("version"))
            Version = _json["version"]!.ToObject<string>()!;
    }

    protected virtual void VerifyTarget()
    {
        if (_json.ContainsKey("target"))
        {
            Target = _json["target"]!.ToObject<string>()!;
            _json.Remove("target");
            if (!_validTargets.Contains(Target))
                throw new Exception($"{Target} is not a valid target");
        }
    }

    protected virtual void VerifyDescription()
    {
        if (_json.ContainsKey("description"))
        {
            Description = _json["description"]!.ToObject<string>()!;
            _json.Remove("description");
        }
    }

    protected virtual void VerifyDependencies()
    {
        if (_json.ContainsKey("dependencies"))
            throw new Exception($"Dependencies are not supported on manifest v{ManifestVersion}");
    }

    protected virtual void VerifyInjects()
    {
        if (!_json.ContainsKey("inject"))
            return;
        
        if (_json["inject"] is not JObject injects)
            throw new Exception("Inject section of themes.json is not a dictionary");
        
        foreach (var (key, value) in injects)
        {
            VerifySingleInject(key, value as JArray);
        }
    }

    protected virtual void VerifySinglePatch(string key, JObject patch)
    {
        if (!patch.ContainsKey("default"))
            throw new Exception($"v{ManifestVersion} patches need to have a default key in a patch");

        string def = patch["default"]!.ToObject<string>()!;

        if (!patch.ContainsKey(def))
            throw new Exception("Default key in patch needs to be present");

        foreach (var (option, injects) in patch)
        {
            if (option == "default")
                continue;

            if (injects is not JObject injectsObj)
                throw new Exception("Value of patch option is not a dictionary");
                
            foreach (var (s, jToken) in injectsObj)
            {
                VerifySingleInject(s, jToken as JArray);
            }
        }
    }

    protected virtual void VerifyPatches()
    {
        if (!_json.ContainsKey("patches"))
            return;
        
        if (_json["patches"] is not JObject patches)
            throw new Exception("Patches section of themes.json is not a dictionary");

        foreach (var (key, value) in patches)
        {
            if (value is not JObject patch)
                throw new Exception("Patch is not of type object");
            
            VerifySinglePatch(key, patch);
        }
    }

    protected virtual void VerifyFlags()
    {
        if (_json.ContainsKey("flags"))
            throw new Exception($"Flags are not supported on manifest v{ManifestVersion}");
    }

    protected virtual void VerifyTabMappings()
    {
        if (_json.ContainsKey("tabs"))
            throw new Exception($"Tab Mappings are not supported on manifest v{ManifestVersion}");
    }

    public virtual void FullVerify()
    {
        VerifyName();
        VerifyAuthor();
        VerifyVersion();
        VerifyTarget();
        VerifyDescription();
        VerifyDependencies();
        VerifyInjects();
        VerifyPatches();
        VerifyFlags();
        VerifyTabMappings();
    }
}