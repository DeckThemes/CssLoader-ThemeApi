using System.IO.Compression;
using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Extensions;
using DeckPersonalisationApi.Model;

namespace DeckPersonalisationApi.Services.Tasks.Common;

public class ExtractZipTask : ITaskPart
{
    public string Name => "Extracting zip";

    private IDirTaskPart _target;
    private SavedBlob _blob;
    private BlobService _blobService;
    private long _maxSize;
    
    public void Execute()
    {
        string path = _blobService.GetFullFilePath(_blob);

        if (!File.Exists(path))
            throw new TaskFailureException("File does not exist");

        long totalSize = 0;
        try
        {
            using (ZipArchive archive = ZipFile.OpenRead(path))
            {
                foreach (var zipArchiveEntry in archive.Entries)
                {
                    totalSize += zipArchiveEntry.Length;

                    if (totalSize > _maxSize)
                    {
                        throw new Exception($"Theme is too big. Themes can be max {_maxSize.GetReadableFileSize()}");
                    }
                }
            }
        }
        catch (InvalidDataException e)
        {
            throw new TaskFailureException("Uploaded file does not seem to be a zip file");
        }
        catch (Exception e)
        {
            throw new TaskFailureException("Unzipping zip failed");
        }
        
        Console.WriteLine($"[Zip] Extracted to {_target.DirPath} with size {totalSize}");
        ZipFile.ExtractToDirectory(path, _target.DirPath);
    }

    public ExtractZipTask(IDirTaskPart target, SavedBlob blob, long maxSize)
    {
        _target = target;
        _blob = blob;
        _maxSize = maxSize;
    }

    public void Cleanup(bool success)
    {
        _blobService.DeleteBlob(_blob.Id);
    }

    public void SetupServices(IServiceProvider provider)
    {
        _blobService = provider.GetRequiredService<BlobService>();
    }
}