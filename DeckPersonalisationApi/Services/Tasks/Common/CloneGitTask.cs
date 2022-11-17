using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Utils;

namespace DeckPersonalisationApi.Services.Tasks.Common;

public class CloneGitTask : IDirTaskPart
{
    public string Name => $"Cloning {_url}@{_commit ?? "Latest"}";

    private string _url;
    private string? _commit;
    private string _workDir;
    private bool _removeGitFolder;
    public Git Repo { get; set; }
    public string DirPath => Repo.Path;

    public void Execute()
    {
        try
        {
            Git.Clone(_url, _workDir).GetAwaiter().GetResult();
        }
        catch (Exception _)
        {
            throw new TaskFailureException("Git clone failed");
        }

        Repo = new(_workDir);

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
            Directory.Delete(Path.Join(_workDir, ".git"), true);
        }
        catch (Exception _)
        {
            // TODO: Gracefully handle error
            //throw new TaskFailureException("Deletion of .git failed");
        }
    }

    public void Cleanup(bool success)
    {
        if (Directory.Exists(_workDir))
            Directory.Delete(_workDir, true);
    }

    public CloneGitTask(string url, string? commit, bool removeGitFolder = false)
    {
        _url = url;
        _commit = commit;
        _workDir = GetTemporaryDirectory();
        _removeGitFolder = removeGitFolder;
    }

    private string GetTemporaryDirectory()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);
        return tempDirectory;
    }
}