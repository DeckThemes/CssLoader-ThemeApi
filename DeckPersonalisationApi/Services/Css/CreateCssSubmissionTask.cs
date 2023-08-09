using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto.External.POST;
using DeckPersonalisationApi.Services.Tasks;
using DeckPersonalisationApi.Services.Tasks.Common;

namespace DeckPersonalisationApi.Services.Css;

public class CreateCssSubmissionTask : ITaskPart
{
    public string Name => "Creating submission";
    private ThemeService _service;
    private SubmissionService _submission;
    private BlobService _blob;
    private ValidateCssThemeTask _validation;
    private WriteAsBlobTask _download;
    private SubmissionMeta _meta;
    private string? _source;
    private User _author;
    private CloneGitTask? _gitSrc;
    public void Execute()
    {
        if (_gitSrc != null)
            _source = $"{_gitSrc.Url} @ {_gitSrc.Commit}";
        
        List<string> blobs = 
            _meta.ImageBlobs ?? new();

        if (blobs.Count <= 0 && _validation.Base != null)
            blobs = _validation.Base.Images.Select(x => _blob.CopyBlob(x)).Select(x => x.Id).ToList();

        string description = _meta.Description ?? _validation.ThemeDescription;
        List<string> targets = _meta.Target == null ? _validation.ThemeTargets : new List<string>() { _meta.Target };

        if (_validation.ThemeFlags.Contains(CssFlag.Preset))
            targets = _validation.ThemeTargets;
        
        CssTheme theme = _service.CreateTheme(_validation.ThemeId, _validation.ThemeName, blobs, _download.Blob.Id, _validation.ThemeVersion,
            _source, _author.Id, targets, _validation.ThemeManifestVersion, description,
            _validation.ThemeDependencies, _validation.ThemeAuthor, ThemeType.Css);

        _submission.CreateSubmission(_validation.Base?.Id ?? null, theme.Id,
            _validation.Base == null ? CssSubmissionIntent.NewTheme : CssSubmissionIntent.UpdateTheme, _author.Id, _validation.Errors);
    }

    public void Cleanup(bool success)
    {
    }

    public CreateCssSubmissionTask(ValidateCssThemeTask validation, WriteAsBlobTask download, SubmissionMeta meta, string? source, User author)
    {
        _validation = validation;
        _download = download;
        _meta = meta;
        _source = source;
        _author = author;
    }
    
    public CreateCssSubmissionTask(ValidateCssThemeTask validation, WriteAsBlobTask download, SubmissionMeta meta, CloneGitTask? source, User author)
    {
        _validation = validation;
        _download = download;
        _meta = meta;
        _gitSrc = source;
        _author = author;
    }
    
    public void SetupServices(IServiceProvider provider)
    {
        _service = provider.GetRequiredService<ThemeService>();
        _submission = provider.GetRequiredService<SubmissionService>();
        _blob = provider.GetRequiredService<BlobService>();
    }
}