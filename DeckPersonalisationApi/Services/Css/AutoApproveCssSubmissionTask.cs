using DeckPersonalisationApi.Services.Tasks;

namespace DeckPersonalisationApi.Services.Css;

public class AutoApproveCssSubmissionTask : ITaskPart
{
    private ThemeService _service;
    private SubmissionService _submission;
    private BlobService _blob;
    private CreateCssSubmissionTask _submissionTask;
    private ValidateCssThemeTask _validateTask;
    
    public string Name => "Testing if submitted theme is available for auto approval";
    public void Execute()
    {
        if (!_validateTask.ThemeFlags.Contains(CssFlag.Preset) || _submissionTask.Submission == null || _validateTask.FileCount != 1)
            return;
        
        _submission.ApproveTheme(_submissionTask.Submission.Id, "API: Auto-Approval of Profile", _submissionTask.Submission.Owner);
    }

    public AutoApproveCssSubmissionTask(CreateCssSubmissionTask submissionTask, ValidateCssThemeTask validateTask)
    {
        _submissionTask = submissionTask;
        _validateTask = validateTask;
    }

    public void SetupServices(IServiceProvider provider)
    {
        _service = provider.GetRequiredService<ThemeService>();
        _submission = provider.GetRequiredService<SubmissionService>();
        _blob = provider.GetRequiredService<BlobService>();
    }
}