namespace DeckPersonalisationApi.Services.Tasks.Common;

public class FolderSizeConstraintTask : ITaskPart
{
    public string Name => "Checking folder size";
    private string _path;
    public void Execute()
    {
        throw new NotImplementedException();
    }

    public void Cleanup(bool success)
    {
        throw new NotImplementedException();
    }

    public FolderSizeConstraintTask(string path)
    {
        _path = path;
    }
}