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
    private AppConfiguration _config;
    private ThemeService _service;
    private SubmissionService _submissionService;
    private VnuCssVerifier _vnu;
    private UserService _userService;

    public string ThemeId { get; private set; }
    public string ThemeName { get; private set; }
    public string? ThemeDisplayName { get; private set; }
    public string ThemeAuthor { get; private set; }
    public string ThemeVersion { get; private set; }
    public List<string> ThemeTargets { get; private set; }
    public CssTheme? Base { get; private set; }
    public int ThemeManifestVersion { get; private set; }
    public string ThemeDescription { get; private set; }
    public List<string> ThemeDependencies { get; private set; } = new();
    public List<string> Errors { get; private set; } = new();
    public List<CssFlag> ThemeFlags { get; private set; } = new();
    
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
            case 5:
                validator = new CssManifestV5Validator(_path.DirPath!, _json.Json!, _user, _validThemeTargets);
                break;
            case 6:
                validator = new CssManifestV6Validator(_path.DirPath!, _json.Json!, _user, _validThemeTargets);
                break;
            case 7:
            case 8:
                validator = new CssManifestV7Validator(_path.DirPath!, _json.Json!, _user, _validThemeTargets);
                break;
            case 9:
                validator = new CssManifestV9Validator(_path.DirPath!, _json.Json!, _user, _validThemeTargets);
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
        ThemeTargets = validator.Targets.Count > 0
            ? validator.Targets
            : (Base?.Targets == null ? new List<string>() { "Other" } : Base.ToReadableTargets());

        ThemeDisplayName = validator.DisplayName;
        ThemeFlags = validator.Flags;
        ThemeAuthor = validator.Author;
        ThemeVersion = validator.Version;
        ThemeManifestVersion = manifestVersion;
        ThemeDescription = validator?.Description ?? Base?.Description ?? "";
        ThemeDependencies = validator!.Dependencies;

        if (ThemeFlags.Contains(CssFlag.Preset))
        {
            ThemeTargets = new() { "Profile" };
            ThemeVersion = Utils.Utils.GetFixedLengthHexString(4);
            _json.Json!["version"] = ThemeVersion;
        }
        else if (ThemeTargets.Contains("Preset") || ThemeTargets.Contains("Profile"))
            throw new TaskFailureException("Target 'Profile' is not a user-pickable value");

        List<CssTheme> t = _service.GetAnyThemesByAuthorWithName(_user, ThemeName, ThemeType.Css).ToList();
        CssTheme? pendingSubmissionTheme = t.FirstOrDefault(x => x.Visibility == PostVisibility.Private);
        if (pendingSubmissionTheme != null)
        {
            CssSubmission? pendingSubmission = _submissionService.GetSubmissionByThemeId(pendingSubmissionTheme.Id);
            if (pendingSubmission != null)
            {
                _submissionService.DenyTheme(pendingSubmission.Id, "Automatically denied due to re-submission.", _userService.GetUserById(_user.Id)!);
            }
        }

        Base = t.FirstOrDefault(x => x.Visibility == PostVisibility.Public);

        if (_service.ThemeNameExists(ThemeName, ThemeType.Css) && Base == null)
            throw new TaskFailureException($"Theme '{ThemeName}' already exists");

        List<CssTheme> dependencies = _service.GetThemesByName(ThemeDependencies, ThemeType.Css).ToList();
        if (dependencies.Count != ThemeDependencies.Count)
        {
            List<string> missingThemeNames = ThemeDependencies.Where(x => dependencies.All(y => y.Name != x)).ToList();
            throw new TaskFailureException($"Not all dependencies were found on this server: [{string.Join(", ", missingThemeNames)}]");
        }

        string guid = Guid.NewGuid().ToString();
        string internalId = Base?.Id ?? guid;
        ThemeId = guid;
        
        _json.Json!["id"] = internalId;

        List<string> extraErrors = new();
        
        if (Base != null && Base.Version == ThemeVersion)
            extraErrors.Add("Theme has same version as base theme");

        if (ThemeName.Length > _config.MaxNameLength)
            throw new TaskFailureException($"Name field can only be max {_config.MaxNameLength} characters");
        
        if (ThemeAuthor.Length > _config.MaxAuthorLength)
            throw new TaskFailureException($"Author field can only be max {_config.MaxNameLength} characters");
        
        if (ThemeVersion.Length > _config.MaxVersionLength)
            throw new TaskFailureException($"Version field can only be max {_config.MaxNameLength} characters");
        
        if (ThemeDescription.Length > _config.MaxDescriptionLength)
            throw new TaskFailureException($"Description field can only be max {_config.MaxNameLength} characters");
        
        Errors = _vnu.ValidateCss(validator.CssPaths, _path.DirPath, extraErrors);
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
        _service = provider.GetRequiredService<ThemeService>();
        _vnu = provider.GetRequiredService<VnuCssVerifier>();
        _config = provider.GetRequiredService<AppConfiguration>();
        _submissionService = provider.GetRequiredService<SubmissionService>();
        _userService = provider.GetRequiredService<UserService>();
    }

    public string Identifier => ThemeName;
}