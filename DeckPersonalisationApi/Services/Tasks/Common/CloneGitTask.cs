using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Utils;

namespace DeckPersonalisationApi.Services.Tasks.Common;

public class CloneGitTask : IDirTaskPart
{
    public string Name => $"Cloning {_url}@{_commit ?? "Latest"}";

    private string _url;
    private string? _commit;
    private IDirTaskPart _workDir;
    private bool _removeGitFolder;
    public Git Repo { get; set; }
    public string DirPath => Repo.Path;

    public void Execute()
    {
        try
        {
            Git.Clone(_url, _workDir.DirPath).GetAwaiter().GetResult();
        }
        catch (Exception _)
        {
            throw new TaskFailureException("Git clone failed");
        }

        Repo = new(_workDir.DirPath);

        if (_commit != null)
        {
            try
            {
                Repo.ResetHard(_commit).GetAwaiter().GetResult();
            }
            catch (Exception _)
            {
                throw new TaskFailureException("Git reset failed");
            }
        }
    }

    public void Cleanup(bool success)
    {
    }

    public CloneGitTask(string url, string? commit, IDirTaskPart workDir, bool removeGitFolder = false)
    {
        _url = url;
        _commit = commit;
        _workDir = workDir;
        _removeGitFolder = removeGitFolder;
    }
}