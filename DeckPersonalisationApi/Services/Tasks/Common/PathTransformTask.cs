using DeckPersonalisationApi.Exceptions;

namespace DeckPersonalisationApi.Services.Tasks.Common;

public class PathTransformTask : IDirTaskPart
{
    public string Name => "Path transform";
    private IDirTaskPart _dir;
    private string? _subPath = null;
    public string DirPath { get; set; }
    public void Execute()
    {
        if (_subPath != null)
        {
            DirPath = Path.Join(_dir.DirPath, _subPath);
        }
        else
        {
            DirPath = _dir.DirPath;
            
            if (!Path.Exists(_dir.DirPath))
                throw new TaskFailureException("Path does not exist");

            if (Directory.GetFiles(_dir.DirPath).Length == 0)
            {
                string[] dirs = Directory.GetDirectories(_dir.DirPath);
                if (dirs.Length == 1)
                    DirPath = dirs[0];
            }
        }
        
        if (!Path.Exists(DirPath))
            throw new TaskFailureException("Path does not exist");
    }

    public void Cleanup(bool success)
    {
    }

    public PathTransformTask(IDirTaskPart dir, string subPath)
    {
        _dir = dir;
        _subPath = subPath;
    }
    
    public PathTransformTask(IDirTaskPart dir)
    {
        _dir = dir;
    }
}