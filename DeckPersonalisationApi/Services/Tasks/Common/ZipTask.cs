using System.IO.Compression;
using DeckPersonalisationApi.Exceptions;

namespace DeckPersonalisationApi.Services.Tasks.Common;

public class ZipTask : IFullPathTaskPart
{
    public string Name => "Zipping folder";
    private IDirTaskPart _dir;
    
    public string FullPath { get; private set; }
    
    public void Execute()
    {
        FullPath = Path.Join(Path.GetTempPath(), $"{Path.GetRandomFileName()}.zip");
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

    public ZipTask(IDirTaskPart dir)
    {
        _dir = dir;
    }
}