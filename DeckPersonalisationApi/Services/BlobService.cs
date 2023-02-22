using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Extensions;
using DeckPersonalisationApi.Model;
using Microsoft.EntityFrameworkCore;

namespace DeckPersonalisationApi.Services;

public class BlobService
{
    private UserService _user;
    private AppConfiguration _conf;
    private ApplicationContext _ctx;
    private ILogger<BlobService> _logger;

    public BlobService(ILogger<BlobService> logger, UserService user, AppConfiguration config, ApplicationContext ctx)
    {
        _logger = logger;
        _user = user;
        _conf = config;
        _ctx = ctx;
    }

    public SavedBlob? GetBlob(string? id)
        => Query().Include(x => x.Owner).FirstOrDefault(x => x.Id == id);

    public IEnumerable<SavedBlob> GetBlobs(List<string> ids, bool strict = true)
    {
        List<SavedBlob> blobs = Query().Where(x => ids.Contains(x.Id)).ToList();
        
        if (blobs.Count != ids.Count)
            _logger.LogWarning($"Failed to get all blob ids! Requested: {StringifyArray(ids)}, Got {blobs.Select(x => x.Id).ToList()}");
        
        if (blobs.Count != ids.Count && strict)
            throw new NotFoundException("Failed to get all blob ids");

        return blobs;
    }

    private string StringifyArray(List<string> items)
        => items.Count > 0 ? $"[\"{string.Join("\",\"", items)}\"]" : "[]";

    public List<SavedBlob> GetBlobsByUser(User user)
        => Query().Where(x => x.Owner == user).ToList();

    public int GetBlobCountByUser(User user)
        => Query().Count(x => x.Owner == user && !x.Confirmed);

    public string GetFullFilePath(SavedBlob blob)
        => Path.Join(blob.Confirmed ? _conf.BlobPath : _conf.TempBlobPath, $"{blob.Id}.{blob.Type.GetExtension()}");

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

    public void DeleteBlob(string blobId)
        => DeleteBlob(GetBlob(blobId).Require());

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

    public SavedBlob CopyBlob(SavedBlob blob)
    {
        string file = GetFullFilePath(blob);

        if (!File.Exists(file))
            throw new Exception("Failed to open original");

        FileStream stream = File.OpenRead(file);
        SavedBlob result = CreateBlob(stream, $"a.{blob.Type.GetExtension()}", blob.Owner.Id);
        stream.Close();
        return result;
    }

    public SavedBlob CreateBlob(Stream blob, string filename, string userId)
    {
        _ctx.ChangeTracker.Clear();
        User user = _user.GetActiveUserById(userId).Require("User not found");
        string ext = filename.Split('.').Last();

        Dictionary<string, long> fileSizeLimits = _conf.ValidFileTypesAndMaxSizes;

        if (!fileSizeLimits.ContainsKey(ext)) // TODO: Check if it's actually a jpg
            throw new BadRequestException("File type is not supported");

        if (blob.Length > fileSizeLimits[ext])
            throw new BadRequestException($"File is too large. Max filesize is {fileSizeLimits[ext].GetReadableFileSize()}");

        if (GetBlobCountByUser(user) > _conf.MaxUnconfirmedBlobs)
            throw new BadRequestException("User has reached the max upload limit");

        BlobType type = BlobTypeEx.FromExtensionToBlobType(ext);
        string id = Guid.NewGuid().ToString();
        string path = $"{id}.{type.GetExtension()}";
        var file = File.Create(Path.Join(_conf.TempBlobPath, path));
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
        List<SavedBlob> unconfirmedBlobs = Query().Where(x => !x.Confirmed).ToList();
        List<SavedBlob> unconfirmedAndExpiredBlobs =
            unconfirmedBlobs.Where(x => (x.Uploaded + TimeSpan.FromMinutes(_conf.BlobTtlMinutes)) < DateTimeOffset.Now).ToList();
        DeleteBlobs(unconfirmedAndExpiredBlobs);
        return unconfirmedAndExpiredBlobs.Count;
    }

    public void WriteDownloadCache(Dictionary<string, int> cache)
    {
        List<SavedBlob> blobs = GetBlobs(cache.Keys.ToList(), false).ToList();
        foreach (var savedBlob in blobs)
        {
            savedBlob.DownloadCount += cache[savedBlob.Id];
            _ctx.Blobs.Update(savedBlob);
        }

        _ctx.SaveChanges();
        _logger.LogInformation($"Updated {blobs.Count} blobs with new downloads");
    }

    private IQueryable<SavedBlob> Query() => _ctx.Blobs.Where(x => !x.Deleted);
}