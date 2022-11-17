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
        if (_file == "*")
        {
            if (Directory.Exists(_src.DirPath) && Directory.Exists(_dst.DirPath))
            {
                CopyFilesRecursively(_src.DirPath, _dst.DirPath);
            }
        }
        
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
    
    private static void CopyFilesRecursively(string sourcePath, string targetPath)
    {
        //Now Create all of the directories
        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        }

        //Copy all the files & Replaces any files with the same name
        foreach (string newPath in Directory.GetFiles(sourcePath, "*.*",SearchOption.AllDirectories))
        {
            File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
        }
    }
}