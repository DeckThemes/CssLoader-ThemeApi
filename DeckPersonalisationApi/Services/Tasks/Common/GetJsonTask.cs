using DeckPersonalisationApi.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DeckPersonalisationApi.Services.Tasks.Common;

public class GetJsonTask : ITaskPart
{
    public string Name => $"Reading data from {_fileName}";
    private PathTransformTask _path;
    private string _fileName;
    private bool _optional;
    public JObject? Json { get; private set; }
    public void Execute()
    {
        string fullPath = Path.Join(_path.Path, _fileName);
        if (!File.Exists(fullPath))
        {
            if (_optional)
                return;

            throw new TaskFailureException($"{_fileName} does not exist");
        }

        try
        {
            Json = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(fullPath))!;

            if (Json == null)
                throw new Exception();
        }
        catch (Exception _)
        {
            throw new TaskFailureException($"Failed to parse {_fileName}");
        }
    }

    public void Cleanup(bool success)
    { }

    public GetJsonTask(PathTransformTask basePath, string file, bool optional = false)
    {
        _path = basePath;
        _fileName = file;
        _optional = optional;
    }
}