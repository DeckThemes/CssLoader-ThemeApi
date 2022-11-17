using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Model;

namespace DeckPersonalisationApi.Services.Tasks.Common;

public class WriteAsBlobTask : ITaskPart
{
    public string Name => "Converting to blob";
    public SavedBlob Blob { get; set; }

    private User _user;
    private BlobService _blob;
    private IDirTaskPart _dir;
    private string _filename;
    
    public void Execute()
    {
        string path = Path.Join(_dir.DirPath, _filename);

        if (!File.Exists(path))
            throw new TaskFailureException("File does not exist");

        Stream file = File.OpenRead(path);
        Blob = _blob.CreateBlob(file, _filename, _user.Id);
    }

    public void Cleanup(bool success)
    {
    }

    public WriteAsBlobTask(User user, BlobService blob, IDirTaskPart dir, string filename)
    {
        _user = user;
        _blob = blob;
        _dir = dir;
        _filename = filename;
    }
}