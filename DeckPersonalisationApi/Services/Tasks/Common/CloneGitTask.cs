using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Utils;

namespace DeckPersonalisationApi.Services.Tasks.Common;

public class CloneGitTask : IDirTaskPart
{
    public string Name => $"Cloning {Url}@{_commit ?? "Latest"}";

    public string Url { get; private set; }
    private string? _commit;
    private IDirTaskPart _workDir;
    public Git Repo { get; set; }
    public string Commit { get; set; }
    public string DirPath => Repo.Path;

    public void Execute()
    {
        try
        {
            Git.Clone(Url, _workDir.DirPath).GetAwaiter().GetResult();
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

        try
        {
            Commit = Repo.GetLatestCommitHash().GetAwaiter().GetResult();
        }
        catch (Exception _)
        {
            throw new TaskFailureException("Failed to get current commit");
        }
    }

    public void Cleanup(bool success)
    {
    }

    public CloneGitTask(string url, string? commit, IDirTaskPart workDir)
    {
        Url = url;
        _commit = commit;
        _workDir = workDir;
    }
}