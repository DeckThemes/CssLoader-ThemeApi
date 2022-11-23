using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto.External.POST;
using DeckPersonalisationApi.Services.Tasks;
using DeckPersonalisationApi.Services.Tasks.Common;

namespace DeckPersonalisationApi.Services.Css;

public class CreateCssSubmissionTask : ITaskPart
{
    public string Name => "Creating submission";
    private CssThemeService _service;
    private CssSubmissionService _submission;
    private BlobService _blob;
    private ValidateCssThemeTask _validation;
    private WriteAsBlobTask _download;
    private CssSubmissionMeta _meta;
    private string? _source;
    private User _author;
    public void Execute()
    {
        List<SavedBlob> blobs = 
            (_meta.ImageBlobs != null) 
                ? _blob.GetBlobs(_meta.ImageBlobs).ToList()
                : new();

        if (blobs.Count <= 0)
            blobs = _validation.Base?.Images ?? new();
        
        _blob.ConfirmBlobs(blobs);
        
        CssTheme theme = _service.CreateTheme(_validation.ThemeId, _validation.ThemeName, blobs, _download.Blob, _validation.ThemeVersion,
            _source, _author, _meta.Target ?? _validation.ThemeTarget, _validation.ThemeManifestVersion, _meta.Description ?? _validation.ThemeDescription,
            _validation.ThemeDependencies, _validation.ThemeAuthor);

        _submission.CreateSubmission(_validation.Base, theme,
            _validation.Base == null ? CssSubmissionIntent.NewTheme : CssSubmissionIntent.UpdateTheme, _author);
    }

    public void Cleanup(bool success)
    {
    }

    public CreateCssSubmissionTask(ValidateCssThemeTask validation, WriteAsBlobTask download, CssSubmissionMeta meta, string? source, User author)
    {
        _validation = validation;
        _download = download;
        _meta = meta;
        _source = source;
        _author = author;
    }
    
    public void SetupServices(IServiceProvider provider)
    {
        _service = provider.GetRequiredService<CssThemeService>();
        _submission = provider.GetRequiredService<CssSubmissionService>();
        _blob = provider.GetRequiredService<BlobService>();
    }
}