namespace DeckPersonalisationApi.Services.Tasks.Common;

public class DeleteAnyFileWithExtensionTask : ITaskPart
{
    public string Name => $"Deleting [{string.Join(',', _extensions)}]";
    private List<string> _extensions;
    private IDirTaskPart _dir;
    public void Execute()
    {
        Exec(_dir.DirPath);
    }

    private void Exec(string path)
    {
        foreach (string file in Directory.EnumerateFiles(path))
        {
            if (_extensions.Any(path.EndsWith))
                File.Delete(file);
        }

        foreach (string folder in Directory.EnumerateDirectories(path))
        {
            Exec(folder);
        }
    }

    public DeleteAnyFileWithExtensionTask(IDirTaskPart dir, string extension) 
        : this(dir, new List<string>(){extension})
    { }
    
    public DeleteAnyFileWithExtensionTask(IDirTaskPart dir, List<string> extensions)
    {
        _extensions = extensions;
        _dir = dir;
    }
}