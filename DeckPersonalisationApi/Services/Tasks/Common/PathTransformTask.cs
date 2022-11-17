using DeckPersonalisationApi.Exceptions;

namespace DeckPersonalisationApi.Services.Tasks.Common;

public class PathTransformTask : IDirTaskPart
{
    public string Name => "Path transform";
    private IDirTaskPart _git;
    private string _subPath;
    public string DirPath { get; set; }
    public void Execute()
    {
        DirPath = Path.Join(_git.DirPath, _subPath);
        if (!Path.Exists(DirPath))
            throw new TaskFailureException("Path does not exist");
    }

    public void Cleanup(bool success)
    {
    }

    public PathTransformTask(IDirTaskPart git, string subPath)
    {
        _git = git;
        _subPath = subPath;
    }
}