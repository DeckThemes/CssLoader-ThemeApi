namespace DeckPersonalisationApi.Services.Tasks.Common;

public class WriteJsonTask : ITaskPart
{
    public string Name => $"Reading data to {_fileName}";
    private PathTransformTask _path;
    private string _fileName;
    private GetJsonTask _json;
    public void Execute()
    {
        string path = Path.Join(_path.Path, _fileName);
        File.WriteAllText(path, _json.Json!.ToString());
    }

    public void Cleanup(bool success)
    {
    }

    public WriteJsonTask(PathTransformTask path, string fileName, GetJsonTask json)
    {
        _path = path;
        _fileName = fileName;
        _json = json;
    }
}