namespace DeckPersonalisationApi.Services.Tasks.Common;

public class CreateFolderTask : IDirTaskPart
{
    public string Name => "Creating directory";
    public string DirPath { get; set; }

    private IDirTaskPart _base;
    private IIdentifierTaskPart _name;
    
    public void Execute()
    {
        DirPath = Path.Join(_base.DirPath, _name.Identifier);
        Directory.CreateDirectory(DirPath);
    }

    public void Cleanup(bool success)
    {
    }

    public CreateFolderTask(IDirTaskPart @base, IIdentifierTaskPart name)
    {
        _base = @base;
        _name = name;
    }
}