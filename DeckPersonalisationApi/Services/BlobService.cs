using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Extensions;
using DeckPersonalisationApi.Model;

namespace DeckPersonalisationApi.Services;

public class BlobService
{
    private UserService _user;
    private IConfiguration _conf;
    private ApplicationContext _ctx;

    public TimeSpan BlobTTLMinutes => TimeSpan.FromMinutes(int.Parse(_conf["Config:BlobTTLMinutes"]!));

    public string BlobDir
    {
        get
        {
            string path = _conf["Config:BlobPath"]!;
            if (!Path.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }
    }

    public string TempBlobDir
    {
        get
        {
            string path = _conf["Config:TempBlobPath"]!;
            if (!Path.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }
    }

    public Dictionary<string, long> FileSizeLimit
    {
        get
        {
            Dictionary<string, long> items = new();
            string conf = _conf["Config:MaxUploadFileSizes"]!;
            foreach (var s in conf.Split(";"))
            {
                string[] i = s.Split(":");
                if (i.Length != 2)
                    throw new Exception("Failed to parse upload file sizes");
                
                items.Add(i[0], long.Parse(i[1]));
            }

            return items;
        }
    }

    public int MaxBlobs => int.Parse(_conf["Config:MaxBlobs"]!);

    public BlobService(UserService user, IConfiguration config, ApplicationContext ctx)
    {
        _user = user;
        _conf = config;
        _ctx = ctx;
    }

    public SavedBlob? GetBlob(string? id)
        => _ctx.Blobs.FirstOrDefault(x => x.Id == id);

    public IEnumerable<SavedBlob> GetBlobs(List<string> ids)
    {
        List<SavedBlob> blobs = _ctx.Blobs.Where(x => ids.Contains(x.Id)).ToList();
        if (blobs.Count != ids.Count)
            throw new NotFoundException("Failed to get all blob ids");

        return blobs;
    }

    public List<SavedBlob> GetBlobsByUser(User user)
        => _ctx.Blobs.Where(x => x.Owner == user).ToList();

    public int GetBlobCountByUser(User user)
        => _ctx.Blobs.Count(x => x.Owner == user && !x.Confirmed);

    public string GetFullFilePath(SavedBlob blob)
        => Path.Join(blob.Confirmed ? BlobDir : TempBlobDir, $"{blob.Id}.{blob.Type.GetExtension()}");

    public void ConfirmBlob(SavedBlob blob)
    {
        ConfirmBlobInternal(blob);
        _ctx.SaveChanges();
    }
    
    public void ConfirmBlobs(List<SavedBlob> blobs)
    {
        if (blobs.Count <= 0)
            return;
        
        blobs.ForEach(ConfirmBlobInternal);
        _ctx.SaveChanges();
    }
    
    private void ConfirmBlobInternal(SavedBlob blob)
    {
        if (blob.Confirmed || blob.Deleted)
            return;

        string oldPath = GetFullFilePath(blob);
        blob.Confirmed = true;
        string newPath = GetFullFilePath(blob);
        
        File.Move(oldPath, newPath);

        _ctx.Blobs.Update(blob);
    }
    
    public void DeleteBlob(SavedBlob blob)
    {
        DeleteBlobInternal(blob);
        _ctx.SaveChanges();
    }

    public void DeleteBlobs(List<SavedBlob> blobs)
    {
        if (blobs.Count <= 0)
            return;
        
        blobs.ForEach(DeleteBlobInternal);
        _ctx.SaveChanges();
    }

    private void DeleteBlobInternal(SavedBlob blob)
    {
        string path = GetFullFilePath(blob);
        if (File.Exists(path))
            File.Delete(path);
        blob.Deleted = true;
        _ctx.Blobs.Update(blob);
    }


    public SavedBlob CreateBlob(Stream blob, string filename, string userId)
    {
        _ctx.ChangeTracker.Clear();
        User user = _user.GetActiveUserById(userId).Require("User not found");
        string ext = filename.Split('.').Last();
        BlobType type = BlobTypeEx.FromExtensionToBlobType(ext);

        Dictionary<string, long> fileSizeLimits = FileSizeLimit;

        if (!fileSizeLimits.ContainsKey(ext)) // TODO: Check if it's actually a jpg
            throw new BadRequestException("File type is not supported");

        if (blob.Length > fileSizeLimits[ext])
            throw new BadRequestException($"File is too large. Max filesize is {fileSizeLimits[ext].GetReadableFileSize()}");

        if (GetBlobCountByUser(user) > MaxBlobs)
            throw new BadRequestException("User has reached the max upload limit");

        string id = Guid.NewGuid().ToString();
        string path = $"{id}.{ext}";
        var file = File.Create(Path.Join(TempBlobDir, path));
        blob.CopyTo(file);
        file.Close();

        SavedBlob result = new()
        {
            Id = id,
            Owner = user,
            Type = type,
            Confirmed = false,
            Uploaded = DateTimeOffset.Now
        };
        
        _ctx.Blobs.Add(result);
        _ctx.SaveChanges();

        return result;
    }

    public int RemoveExpiredBlobs()
    {
        List<SavedBlob> unconfirmedBlobs = _ctx.Blobs.Where(x => !x.Confirmed).ToList();
        List<SavedBlob> unconfirmedAndExpiredBlobs =
            unconfirmedBlobs.Where(x => (x.Uploaded + BlobTTLMinutes) < DateTimeOffset.Now).ToList();
        DeleteBlobs(unconfirmedAndExpiredBlobs);
        return unconfirmedAndExpiredBlobs.Count;
    }
}