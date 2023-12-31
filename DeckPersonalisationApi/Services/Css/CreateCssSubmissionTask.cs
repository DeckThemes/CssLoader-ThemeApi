using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto.External.POST;
using DeckPersonalisationApi.Services.Tasks;
using DeckPersonalisationApi.Services.Tasks.Common;

namespace DeckPersonalisationApi.Services.Css;

public abstract class CreateCssSubmissionTask : ITaskPart
{
    public string Name => "Creating submission";
    protected ThemeService _service;
    protected SubmissionService _submission;
    protected BlobService _blob;
    protected ValidateCssThemeTask _validation;
    protected WriteAsBlobTask _download;
    protected SubmissionMeta _meta;
    protected User _author;
    protected CloneGitTask? _gitSrc;
    
    protected string? _source;
    protected string? _description;
    protected List<string> _blobs;
    protected List<string> _targets;
    
    public CssSubmission? Submission { get; protected set; }
    public void Execute()
    {
        if (_gitSrc != null)
            _source = $"{_gitSrc.Url} @ {_gitSrc.Commit}";
        
        _blobs = _meta.ImageBlobs ?? new();

        if (_blobs.Count <= 0 && _validation.Base != null)
            _blobs = _validation.Base.Images.Select(x => _blob.CopyBlob(x)).Select(x => x.Id).ToList();

        _description = _meta.Description ?? _validation.ThemeDescription;
        _targets = _meta.Target == null ? _validation.ThemeTargets : new List<string>() { _meta.Target };

        if (_validation.ThemeFlags.Contains(CssFlag.Preset))
            _targets = _validation.ThemeTargets;
        
        CreateSubmission();
    }

    protected abstract void CreateSubmission();

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

public class CreatePublicCssSubmissionTask : CreateCssSubmissionTask
{
    public CreatePublicCssSubmissionTask(ValidateCssThemeTask validation, WriteAsBlobTask download, SubmissionMeta meta, string? source, User author) : base(validation, download, meta, source, author)
    {
    }

    public CreatePublicCssSubmissionTask(ValidateCssThemeTask validation, WriteAsBlobTask download, SubmissionMeta meta, CloneGitTask? source, User author) : base(validation, download, meta, source, author)
    {
    }

    protected override void CreateSubmission()
    {
        CssTheme theme = _service.CreateTheme(_validation.ThemeId, _validation.ThemeName, _blobs, _download.Blob.Id, _validation.ThemeVersion,
            _source, _author.Id, _targets, _validation.ThemeManifestVersion, _description!,
            _validation.ThemeDependencies, _validation.ThemeAuthor, ThemeType.Css, _validation.ThemeDisplayName);

        Submission = _submission.CreateSubmission(_validation.Base?.Id ?? null, theme.Id,
            _validation.Base == null ? CssSubmissionIntent.NewTheme : CssSubmissionIntent.UpdateTheme, _author.Id, _validation.Errors);
    }
}

public class CreatePrivateCssSubmissionTask : CreateCssSubmissionTask
{
    public CreatePrivateCssSubmissionTask(ValidateCssThemeTask validation, WriteAsBlobTask download, SubmissionMeta meta, string? source, User author) : base(validation, download, meta, source, author)
    {
    }

    public CreatePrivateCssSubmissionTask(ValidateCssThemeTask validation, WriteAsBlobTask download, SubmissionMeta meta, CloneGitTask? source, User author) : base(validation, download, meta, source, author)
    {
    }

    protected override void CreateSubmission()
    {
        if (_validation.Base != null)
        {
            _service.ApplyThemeUpdate(_validation.Base.Id, _blobs, _download.Blob.Id, _validation.ThemeVersion,
                _source, _author.Id, _targets, _validation.ThemeManifestVersion, _description!,
                _validation.ThemeDependencies, _validation.ThemeAuthor, ThemeType.Css, _validation.ThemeDisplayName);
        }
        else
        {
            _service.CreateTheme(_validation.ThemeId, _validation.ThemeName, _blobs, _download.Blob.Id, _validation.ThemeVersion,
                _source, _author.Id, _targets, _validation.ThemeManifestVersion, _description!,
                _validation.ThemeDependencies, _validation.ThemeAuthor, ThemeType.Css, _validation.ThemeDisplayName);
        }
    }
}