using System.IO.Compression;
using DeckPersonalisationApi.Exceptions;

namespace DeckPersonalisationApi.Services.Tasks.Common;

public class ZipTask : IFullPathTaskPart
{
    public string Name => "Zipping folder";
    private IDirTaskPart _dir;
    private IDirTaskPart _workDir;
    
    public string FullPath { get; private set; }
    
    public void Execute()
    {
        FullPath = Path.Join(_workDir.DirPath, $"{Path.GetRandomFileName()}.zip");
        try
        {
            ZipFile.CreateFromDirectory(_dir.DirPath, FullPath);
        }
        catch
        {
            throw new TaskFailureException("Creating zip file failed");
        }
    }

    public void Cleanup(bool success)
    {
        if (File.Exists(FullPath))
            File.Delete(FullPath);
    }

    public ZipTask(IDirTaskPart dir, IDirTaskPart workDir)
    {
        _dir = dir;
        _workDir = workDir;
    }
}