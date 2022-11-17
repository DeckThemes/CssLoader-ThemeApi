namespace DeckPersonalisationApi.Services.Tasks.Common;

public class CopyFileTask : ITaskPart
{
    public string Name => $"Copying {_file}";
    private IDirTaskPart _src;
    private IDirTaskPart _dst;
    private string _file;
    private bool _overwrite;
    
    public void Execute()
    {
        string src = Path.Join(_src.DirPath, _file);
        string dst = Path.Join(_dst.DirPath, _file);

        if (File.Exists(dst) && !_overwrite)
            return;
        
        if (File.Exists(src))
            File.Copy(src, dst, true);
    }

    public void Cleanup(bool success)
    {
    }
    
    public CopyFileTask(IDirTaskPart src, IDirTaskPart dst, string file, bool overwrite = false)
    {
        _src = src;
        _dst = dst;
        _file = file;
        _overwrite = overwrite;
    }
}