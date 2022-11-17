using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Extensions;

namespace DeckPersonalisationApi.Services.Tasks.Common;

public class FolderSizeConstraintTask : ITaskPart
{
    public string Name => "Checking folder size";
    private IDirTaskPart _dir;
    private long _size;
    public void Execute()
    {
        if (!Directory.Exists(_dir.DirPath))
            throw new TaskFailureException("Path does not exist");

        if (Utils.Utils.DirSize(_dir.DirPath) > _size)
            throw new TaskFailureException($"Folder size is too large: Max allowed is {_size.GetReadableFileSize()}");
    }

    public void Cleanup(bool success)
    {
    }

    public FolderSizeConstraintTask(IDirTaskPart dir, long size)
    {
        _dir = dir;
        _size = size;
    }
}