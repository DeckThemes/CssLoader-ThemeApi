namespace DeckPersonalisationApi.Services.Tasks.Common;

public class CreateTempFolderTask : IDirTaskPart
{
    public string Name => "Creating temporary dir";
    public string DirPath { get; set; }
    public void Execute()
    {
        DirPath = GetTemporaryDirectory();
    }

    public void Cleanup(bool success)
    {
        if (Directory.Exists(DirPath))
            Directory.Delete(DirPath, true);
    }
    
    private string GetTemporaryDirectory()
    {
        string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDirectory);
        return tempDirectory;
    }
}