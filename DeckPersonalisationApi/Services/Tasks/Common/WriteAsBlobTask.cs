using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Model;

namespace DeckPersonalisationApi.Services.Tasks.Common;

public class WriteAsBlobTask : ITaskPart
{
    public string Name => "Converting to blob";
    public SavedBlob Blob { get; set; }

    private User _user;
    private BlobService _blob;
    private IFullPathTaskPart _file;

    public void Execute()
    {
        string path = _file.FullPath;

        if (!File.Exists(path))
            throw new TaskFailureException("File does not exist");

        Stream file = File.OpenRead(path);
        Blob = _blob.CreateBlob(file, Path.GetFileName(path), _user.Id);
    }

    public void Cleanup(bool success)
    {
    }

    public WriteAsBlobTask(User user, BlobService blob, IFullPathTaskPart file)
    {
        _user = user;
        _blob = blob;
        _file = file;
    }
}