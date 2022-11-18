using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Services.Tasks;
using DeckPersonalisationApi.Services.Tasks.Common;

namespace DeckPersonalisationApi.Services.Css;

public class CreateCssSubmissionTask : ITaskPart
{
    public string Name => "Creating submission";
    private CssThemeService _service;
    private ValidateCssThemeTask _validation;
    private WriteAsBlobTask _download;
    private List<string> _imageIds;
    private string? _source;
    private User _author;
    public void Execute()
    {
        _service.CreateSubmission(_validation.ThemeId, _validation.ThemeName, _imageIds, _download.Blob, _validation.ThemeVersion,
            _source, _author, _validation.ThemeTarget, _validation.ThemeManifestVersion, _validation.ThemeDescription,
            _validation.ThemeDependencies, _validation.ThemeAuthor);
    }

    public void Cleanup(bool success)
    {
    }

    public CreateCssSubmissionTask(ValidateCssThemeTask validation, WriteAsBlobTask download, List<string> imageIds, string? source, User author)
    {
        _validation = validation;
        _download = download;
        _imageIds = imageIds;
        _source = source;
        _author = author;
    }
    
    public void SetupServices(IServiceProvider provider)
    {
        _service = provider.GetRequiredService<CssThemeService>();
    }
}