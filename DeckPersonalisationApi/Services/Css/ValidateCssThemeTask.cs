using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Services.Tasks;
using DeckPersonalisationApi.Services.Tasks.Common;

namespace DeckPersonalisationApi.Services.Css;

public class ValidateCssThemeTask : ITaskPart
{
    public string Name => "Validating css theme";
    private PathTransformTask _path;
    private GetJsonTask _json;
    private User _user;
    private IConfiguration _config;
    private string _themeId;
    private List<string> _validThemeTargets = new();
    
    public string ThemeName { get; private set; }
    public string ThemeAuthor { get; private set; }
    public string ThemeVersion { get; private set; }
    public string ThemeTarget { get; private set; }
    public int ThemeManifestVersion { get; private set; }
    public string ThemeDescription { get; private set; }
    public List<string> ThemeDependencies { get; private set; } = new();
    
    
    public void Execute()
    {
        _validThemeTargets = _config["Config:CssTargets"]!.Split(';').ToList();
        
        CssManifestV1Validator validator;

        int manifestVersion = 1;

        if (_json.Json!.ContainsKey("manifest_version"))
            manifestVersion = _json.Json!["manifest_version"]!.ToObject<int>();

        switch (manifestVersion)
        {
            case 1:
                validator = new CssManifestV1Validator(_path.Path!, _json.Json!, _user, _validThemeTargets);
                break;
            case 2:
                validator = new CssManifestV2Validator(_path.Path!, _json.Json!, _user, _validThemeTargets);
                break;
            case 3:
                validator = new CssManifestV3Validator(_path.Path!, _json.Json!, _user, _validThemeTargets);
                break;
            case 4:
                validator = new CssManifestV4Validator(_path.Path!, _json.Json!, _user, _validThemeTargets);
                break;
            default:
                throw new TaskFailureException($"Invalid manifest version '{manifestVersion}'");
        }
        
        _json.Json!["id"] = _themeId;

        try
        {
            validator.FullVerify();
        }
        catch (Exception e)
        {
            throw new TaskFailureException(e.Message);
        }

        ThemeName = validator.Name;
        ThemeAuthor = validator.Author;
        ThemeVersion = validator.Version;
        ThemeTarget = validator.Target;
        ThemeManifestVersion = manifestVersion;
        ThemeDescription = validator.Description;
        ThemeDependencies = validator.Dependencies;
    }

    public void Cleanup(bool success)
    {
    }

    public ValidateCssThemeTask(PathTransformTask path, GetJsonTask json, User user, IConfiguration config, string themeId)
    {
        // TODO: Validate dependencies' existence
        _path = path;
        _json = json;
        _user = user;
        _config = config;
        _themeId = themeId;
    }
}