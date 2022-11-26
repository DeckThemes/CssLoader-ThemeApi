using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Extensions;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto.External.POST;
using DeckPersonalisationApi.Services.Tasks;
using DeckPersonalisationApi.Services.Tasks.Common;

namespace DeckPersonalisationApi.Services.Css;

public class ValidateCssThemeTask : IIdentifierTaskPart
{
    public string Name => "Validating css theme";
    private IDirTaskPart _path;
    private GetJsonTask _json;
    private User _user;
    private List<string> _validThemeTargets = new();
    private CssThemeService _service;
    private VnuCssVerifier _vnu;

    public string ThemeId { get; private set; }
    public string ThemeName { get; private set; }
    public string ThemeAuthor { get; private set; }
    public string ThemeVersion { get; private set; }
    public string ThemeTarget { get; private set; }
    public CssTheme? Base { get; private set; }
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

        if (!_vnu.ValidateCss(validator.CssPaths, _path.DirPath))
            throw new TaskFailureException("Some Css files contain invalid syntax");
        
        ThemeName = validator.Name;

        List<CssTheme> foundThemes = _service.GetThemesByName(new List<string> {ThemeName}).ToList();
        CssTheme? theme = foundThemes.FirstOrDefault(x => x.Author.Id == _user.Id);
        Base = (theme == null) ? null : _service.GetThemeById(theme.Id).Require();

        if (_service.ThemeNameExists(ThemeName) && theme == null)
            throw new TaskFailureException($"Theme '{ThemeName}' already exists");
        
        ThemeAuthor = validator.Author;
        ThemeVersion = validator.Version;
        ThemeTarget = validator?.Target ?? theme?.Target ?? "Other";
        ThemeManifestVersion = manifestVersion;
        ThemeDescription = validator?.Description ?? theme?.Description ?? "";
        ThemeDependencies = validator!.Dependencies;

        List<CssTheme> dependencies = _service.GetThemesByName(ThemeDependencies).ToList();
        if (dependencies.Count != ThemeDependencies.Count)
            throw new TaskFailureException("Not all dependencies were found on this server");

        string guid = Guid.NewGuid().ToString();
        string internalId = theme?.Id ?? guid;
        ThemeId = guid;
        
        _json.Json!["id"] = internalId;
    }

    public void Cleanup(bool success)
    {
    }

    public ValidateCssThemeTask(IDirTaskPart path, GetJsonTask json, User user, List<string> validThemeTargets)
    {
        _path = path;
        _json = json;
        _user = user;
        _validThemeTargets = validThemeTargets;
    }
    
    public void SetupServices(IServiceProvider provider)
    {
        _service = provider.GetRequiredService<CssThemeService>();
        _vnu = provider.GetRequiredService<VnuCssVerifier>();
    }

    public string Identifier => ThemeName;
}