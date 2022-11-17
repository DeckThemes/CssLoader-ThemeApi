using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Services.Css;
using DeckPersonalisationApi.Services.Tasks;
using DeckPersonalisationApi.Services.Tasks.Common;

namespace DeckPersonalisationApi.Services;

public class CssThemeService
{
    private TaskService _task;
    private ApplicationContext _ctx;
    private BlobService _blob;
    private UserService _user;
    private IConfiguration _config;

    public CssThemeService(TaskService task, ApplicationContext ctx, BlobService blob, UserService user, IConfiguration config)
    {
        _task = task;
        _ctx = ctx;
        _blob = blob;
        _user = user;
        _config = config;
    }

    public string SubmitThemeViaGit(string url, string? commit, string subfolder, string userId)
    {
        User? user = _user.GetActiveUserById(userId);
        string id = Guid.NewGuid().ToString();

        List<string> validThemeTargets = _config["Config:CssTargets"]!.Split(';').ToList();

        if (user == null)
            throw new UnauthorisedException("User not found");

        CloneGitTask clone = new CloneGitTask(url, commit, true);
        PathTransformTask folder = new PathTransformTask(clone, subfolder);
        GetJsonTask jsonGet = new GetJsonTask(folder, "theme.json");
        ValidateCssThemeTask css = new ValidateCssThemeTask(folder, jsonGet, user, validThemeTargets, id);
        WriteJsonTask jsonWrite = new WriteJsonTask(folder, "theme.json", jsonGet);

        List<ITaskPart> taskParts = new()
        {
            clone, folder, jsonGet, css, jsonWrite
        };

        AppTaskFromParts task = new(taskParts, "Submit theme via git", user);
        return _task.RegisterTask(task);
    }
}