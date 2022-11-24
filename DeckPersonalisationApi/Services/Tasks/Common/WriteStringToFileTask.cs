namespace DeckPersonalisationApi.Services.Tasks.Common;

public class WriteStringToFileTask : ITaskPart
{
    public string Name => $"Saving file {_filename}";

    private IDirTaskPart _dir;
    private string _filename;
    private string _content;
    
    public void Execute()
    {
        File.WriteAllText(Path.Join(_dir.DirPath, _filename), _content);
    }

    public WriteStringToFileTask(IDirTaskPart dir, string filename, string content)
    {
        _dir = dir;
        _filename = filename;
        _content = content;
    }
}