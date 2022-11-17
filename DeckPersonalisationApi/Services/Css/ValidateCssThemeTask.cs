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
    private List<string> _validThemeTargets = new();
    private CssThemeService _service;
    
    public string ThemeId { get; private set; }
    public string ThemeName { get; private set; }
    public string ThemeAuthor { get; private set; }
    public string ThemeVersion { get; private set; }
    public string ThemeTarget { get; private set; }
    public int ThemeManifestVersion { get; private set; }
    public string ThemeDescription { get; private set; }
    public List<string> ThemeDependencies { get; private set; } = new();
    
    
    public void Execute()
    {
        CssManifestV1Validator validator;

        int manifestVersion = 1;

        if (_json.Json!.ContainsKey("manifest_version"))
            manifestVersion = _json.Json!["manifest_version"]!.ToObject<int>();

        switch (manifestVersion)
        {
            case 1:
                validator = new CssManifestV1Validator(_path.DirPath!, _json.Json!, _user, _validThemeTargets);
                break;
            case 2:
                validator = new CssManifestV2Validator(_path.DirPath!, _json.Json!, _user, _validThemeTargets);
                break;
            case 3:
                validator = new CssManifestV3Validator(_path.DirPath!, _json.Json!, _user, _validThemeTargets);
                break;
            case 4:
                validator = new CssManifestV4Validator(_path.DirPath!, _json.Json!, _user, _validThemeTargets);
                break;
            default:
                throw new TaskFailureException($"Invalid manifest version '{manifestVersion}'");
        }
        
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
        
        List<CssTheme> authorThemes = _service.GetUsersThemes(_user).ToList();
        CssTheme? theme = authorThemes.FirstOrDefault(x => x.Name == ThemeName);

        if (_service.ThemeNameExists(ThemeName) && theme == null)
            throw new TaskFailureException($"Theme '{ThemeName}' already exists");

        // These values can be changed separately from a theme upload, and only will be used for an initial submission if they exist
        if (theme != null)
        {
            ThemeDescription = theme.Description;
            ThemeTarget = theme.Target;
        }
        
        List<CssTheme> dependencies = _service.GetThemesByName(ThemeDependencies).ToList();
        if (dependencies.Count != ThemeDependencies.Count)
            throw new TaskFailureException("Not all dependencies were found on this server");

        ThemeId = theme?.Id ?? Guid.NewGuid().ToString();
        
        _json.Json!["id"] = ThemeId;
    }

    public void Cleanup(bool success)
    {
    }

    public ValidateCssThemeTask(PathTransformTask path, GetJsonTask json, User user, List<string> validThemeTargets)
    {
        _path = path;
        _json = json;
        _user = user;
        _validThemeTargets = validThemeTargets;
    }
    
    public void SetupServices(IServiceProvider provider)
    {
        _service = provider.GetRequiredService<CssThemeService>();
    }
}