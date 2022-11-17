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
        _service.CreateSubmission(_validation.ThemeId, _validation.Name, _imageIds, _download.Blob, _validation.ThemeVersion,
            _source, _author, _validation.ThemeTarget, _validation.ThemeManifestVersion, _validation.ThemeDescription,
            _validation.ThemeDependencies);
    }

    public void Cleanup(bool success)
    {
    }

    public CreateCssSubmissionTask(CssThemeService service, ValidateCssThemeTask validation, WriteAsBlobTask download, List<string> imageIds, string? source, User author)
    {
        _service = service;
        _validation = validation;
        _download = download;
        _imageIds = imageIds;
        _source = source;
        _author = author;
    }
}