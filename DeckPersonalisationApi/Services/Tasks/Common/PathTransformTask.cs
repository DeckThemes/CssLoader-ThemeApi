using DeckPersonalisationApi.Exceptions;

namespace DeckPersonalisationApi.Services.Tasks.Common;

public class PathTransformTask : ITaskPart
{
    public string Name => "Path transform";
    private CloneGitTask _git;
    private string _subPath;
    public string Path { get; set; }
    public void Execute()
    {
        Path = System.IO.Path.Join(_git.Repo.Path, _subPath);
        if (!System.IO.Path.Exists(Path))
            throw new TaskFailureException("Path does not exist");
    }

    public void Cleanup(bool success)
    {
    }

    public PathTransformTask(CloneGitTask git, string subPath)
    {
        _git = git;
        _subPath = subPath;
    }
}