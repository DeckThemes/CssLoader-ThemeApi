using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto.External.POST;
using DeckPersonalisationApi.Services.Css;
using DeckPersonalisationApi.Services.Tasks;
using DeckPersonalisationApi.Services.Tasks.Common;

namespace DeckPersonalisationApi.Services.Audio;

public class CreateAudioSubmissionTask : ITaskPart
{
    public string Name => "Creating submission";
    private ThemeService _service;
    private SubmissionService _submission;
    private BlobService _blob;
    private ValidateAudioPackTask _validation;
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
        
        CssTheme theme = _service.CreateTheme(_validation.PackId, _validation.PackName, blobs, _download.Blob.Id, _validation.PackVersion,
            _source, _author.Id, _validation.IsMusicPack ? "Music" : "Audio", _validation.PackManifestVersion, _meta.Description ?? _validation.PackDescription,
            new(), _validation.PackAuthor, ThemeType.Audio);

        _submission.CreateSubmission(_validation.Base?.Id ?? null, theme.Id,
            _validation.Base == null ? CssSubmissionIntent.NewTheme : CssSubmissionIntent.UpdateTheme, _author.Id, new());
    }
    
    public CreateAudioSubmissionTask(ValidateAudioPackTask validation, WriteAsBlobTask download, SubmissionMeta meta, string? source, User author)
    {
        _validation = validation;
        _download = download;
        _meta = meta;
        _source = source;
        _author = author;
    }
    
    public CreateAudioSubmissionTask(ValidateAudioPackTask validation, WriteAsBlobTask download, SubmissionMeta meta, CloneGitTask? source, User author)
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