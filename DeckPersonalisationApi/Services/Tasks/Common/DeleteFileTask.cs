namespace DeckPersonalisationApi.Services.Tasks.Common;

public class DeleteFileTask : ITaskPart
{
    public string Name => $"Deleting {_fileName}";
    private string _fileName;
    private IDirTaskPart _dir;
    public void Execute()
    {
        string path = Path.Join(_dir.DirPath, _fileName);
        if (File.Exists(path))
            File.Delete(path);
    }

    public DeleteFileTask(IDirTaskPart dir, string fileName)
    {
        _fileName = fileName;
        _dir = dir;
    }
}