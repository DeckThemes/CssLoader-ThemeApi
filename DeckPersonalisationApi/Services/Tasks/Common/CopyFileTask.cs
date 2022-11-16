namespace DeckPersonalisationApi.Services.Tasks.Common;

public class CopyFileTask : ITaskPart
{
    public string Name => $"Copying {_file}";
    private PathTransformTask _src;
    private PathTransformTask _dst;
    private string _file;
    private bool _overwrite;
    
    public void Execute()
    {
        string src = Path.Join(_src.Path, _file);
        string dst = Path.Join(_dst.Path, _file);

        if (File.Exists(dst) && !_overwrite)
            return;
        
        if (File.Exists(src))
            File.Copy(src, dst, true);
    }

    public void Cleanup(bool success)
    {
    }
    
    public CopyFileTask(PathTransformTask src, PathTransformTask dst, string file, bool overwrite = false)
    {
        _src = src;
        _dst = dst;
        _file = file;
        _overwrite = overwrite;
    }
}