using DeckPersonalisationApi.Services.Tasks;

namespace DeckPersonalisationApi.Services;

public class TaskService
{
    private Dictionary<string, AppTask> _tasks = new();
    private IServiceProvider _services;
    private Dictionary<string, int> _blobDlCache = new();

    public TaskService(IServiceProvider services)
    {
        _services = services;
    }

    public AppTask? GetTask(string id)
        => _tasks.ContainsKey(id) ? _tasks[id] : null;

    public string RegisterTask(AppTask task)
    {
        task.Id = Guid.NewGuid().ToString();
        _tasks.Add(task.Id, task);
        Thread thread = new(() => RunTask(task));
        thread.Start();
        return task.Id;
    }

    private void RunTask(AppTask task)
    {
        using (var scope = _services.CreateScope())
        {
            IServiceProvider provider = scope.ServiceProvider;
            task.SetupServices(provider);
            task.Run();
        }
    }
    
    public void RegisterDownload(string blobId)
    {
        if (!_blobDlCache.ContainsKey(blobId))
            _blobDlCache[blobId] = 1;
        else
            _blobDlCache[blobId]++;
    }

    public Dictionary<string, int> RolloverRegisteredDownloads()
    {
        Dictionary<string, int> cache = _blobDlCache;
        _blobDlCache = new();
        return cache;
    }
}