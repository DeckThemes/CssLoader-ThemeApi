using DeckPersonalisationApi.Model;
using Newtonsoft.Json.Linq;

namespace DeckPersonalisationApi.Services.Audio;

public class AudioManifestV1Validator
{
    protected string _packPath;
    protected JObject _json;
    protected List<string> _validFiles;
    
    public string Name { get; protected set; }
    public string Description { get; protected set; } = "";
    public string Author { get; protected set; }
    public string Version { get; protected set; } = "v1.0";
    public int ManifestVersion { get; protected set; }
    public bool Music { get; protected set; } = false;
    public List<string> Ignore { get; protected set; } = new();
    public Dictionary<string, List<string>> Mappings { get; protected set; } = new();
    
    public AudioManifestV1Validator(string packPath, JObject json, User user, List<string> validFiles)
    {
        _packPath = packPath;
        _json = json;
        Author = user.Username;
        _validFiles = validFiles;
        ManifestVersion = 1;
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
    
    protected virtual void VerifyMusicBool()
    {
        if (_json.ContainsKey("music"))
            Music = _json["music"]!.ToObject<bool>()!;
    }
    
    protected virtual void VerifyDescription()
    {
        if (_json.ContainsKey("description"))
        {
            Description = _json["description"]!.ToObject<string>()!;
            _json.Remove("description");
        }
    }

    protected virtual void VerifyMappings()
    {
        if (_json.ContainsKey("mappings"))
            throw new Exception("Mappings are not supported by v1 manifests");
    }
    
    protected virtual void CalculateIgnores()
    {
        foreach (var validFile in _validFiles)
        {
            string path = Path.Join(_packPath, validFile);

            if (Mappings.ContainsKey(validFile) && Path.Exists(path))
                throw new Exception($"{validFile} is found in both mappings and path");
            
            if (!Mappings.ContainsKey(validFile) && !Path.Exists(path))
                Ignore.Add(validFile);
        }

        if (_json.ContainsKey("ignore"))
            _json.Remove("ignore");
        
        _json.Add("ignore", new JArray(Ignore.ToArray()));
    }

    protected virtual void VerifyIntroMusic()
    {
        if (File.Exists("intro_music.mp3") || Mappings.ContainsKey("intro_music.mp3"))
            throw new Exception($"Intro music is not supported on manifest version {ManifestVersion}");
    }
    
    public virtual void FullVerify()
    {
        VerifyName();
        VerifyAuthor();
        VerifyVersion();
        VerifyDescription();
        VerifyMusicBool();

        if (Music)
            _validFiles = new() { "menu_music.mp3", "intro_music.mp3" };
        
        VerifyMappings();
        CalculateIgnores();
        VerifyIntroMusic();

        if (Music && Ignore.Contains("menu_music.mp3"))
            throw new Exception("menu_music.mp3 is not present and is required for a music pack");
    }
}
