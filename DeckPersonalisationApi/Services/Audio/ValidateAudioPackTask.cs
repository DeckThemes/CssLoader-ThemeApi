using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Services.Tasks;
using DeckPersonalisationApi.Services.Tasks.Common;

namespace DeckPersonalisationApi.Services.Audio;

public class ValidateAudioPackTask : IIdentifierTaskPart
{
    private ThemeService _service;
    private IDirTaskPart _path;
    private GetJsonTask _json;
    private User _user;
    private List<string> _validPackTargets = new();
    private AppConfiguration _config;
    
    public string PackId { get; private set; }
    public string PackName { get; private set; }
    public string PackVersion { get; private set; }
    public string PackAuthor { get; private set; }
    public bool IsMusicPack { get; private set; }
    public CssTheme? Base { get; private set; }
    public int PackManifestVersion { get; private set; }
    public string PackDescription { get; private set; }
    public string Name => "Validating audio pack";
    public string Identifier => PackName;

    public ValidateAudioPackTask(IDirTaskPart path, GetJsonTask json, User user, List<string> validPackTargets)
    {
        _path = path;
        _json = json;
        _user = user;
        _validPackTargets = validPackTargets;
    }

    public void Execute()
    {
        AudioManifestV1Validator validator;

        PackManifestVersion = 1;
        
        if (_json.Json!.ContainsKey("manifest_version"))
            PackManifestVersion = _json.Json!["manifest_version"]!.ToObject<int>();

        switch (PackManifestVersion)
        {
            case 1:
                validator = new AudioManifestV1Validator(_path.DirPath!, _json.Json!, _user, _validPackTargets);
                break;
            case 2:
                validator = new AudioManifestV2Validator(_path.DirPath!, _json.Json!, _user, _validPackTargets);
                break;
            default:
                throw new TaskFailureException($"Invalid manifest version '{PackManifestVersion}'");
        }
        
        try
        {
            validator.FullVerify();
        }
        catch (Exception e)
        {
            throw new TaskFailureException(e.Message);
        }
        
        PackName = validator.Name;

        List<CssTheme> t = _service.GetAnyThemesByAuthorWithName(_user, PackName, ThemeType.Audio).ToList();
        if (t.Any(x => x.Visibility == PostVisibility.Private))
            throw new TaskFailureException("Theme seems to already be a pending submission for this theme");
        
        Base = t.FirstOrDefault();

        if (_service.ThemeNameExists(PackName, ThemeType.Audio) && Base == null)
            throw new TaskFailureException($"Theme '{PackName}' already exists");
        
        PackAuthor = validator.Author;
        PackVersion = validator.Version;
        IsMusicPack = validator.Music;
        PackDescription = validator?.Description ?? Base?.Description ?? "";
        
        string guid = Guid.NewGuid().ToString();
        string internalId = Base?.Id ?? guid;
        PackId = guid;
        
        _json.Json!["id"] = internalId;
        
        if (PackName.Length > _config.MaxNameLength)
            throw new TaskFailureException($"Name field can only be max {_config.MaxNameLength} characters");
        
        if (PackAuthor.Length > _config.MaxAuthorLength)
            throw new TaskFailureException($"Author field can only be max {_config.MaxNameLength} characters");
        
        if (PackVersion.Length > _config.MaxVersionLength)
            throw new TaskFailureException($"Version field can only be max {_config.MaxNameLength} characters");
        
        if (PackDescription.Length > _config.MaxDescriptionLength)
            throw new TaskFailureException($"Description field can only be max {_config.MaxNameLength} characters");
    }
    public void SetupServices(IServiceProvider provider)
    {
        _service = provider.GetRequiredService<ThemeService>();
        _config = provider.GetRequiredService<AppConfiguration>();
    }
}