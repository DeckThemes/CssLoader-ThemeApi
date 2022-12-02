using DeckPersonalisationApi.Services.Tasks;

namespace DeckPersonalisationApi.Services;

public class TaskService
{
    private Dictionary<string, AppTask> _tasks = new();
    private IServiceProvider _services;
    private Dictionary<string, int> _blobDlCache = new();
    private AppConfiguration _config;
    private bool _lock = false;

    public TaskService(IServiceProvider services, AppConfiguration config)
    {
        _services = services;
        _config = config;
    }

    public AppTask? GetTask(string id)
        => _tasks.ContainsKey(id) ? _tasks[id] : null;

    public string RegisterTask(AppTask task)
    {
        task.Id = Guid.NewGuid().ToString();
        _tasks.Add(task.Id, task);
        StartNewTask();
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

        _lock = false;
        StartNewTask();
    }

    private void StartNewTask()
    {
        if (_lock)
            return;

        _lock = true;
        
        var task = _tasks.Values.FirstOrDefault(x => x.TaskStarted == null);
        if (task != null)
        {
            Thread thread = new(() => RunTask(task));
            thread.Start();
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

    public void ClearOldTasks()
    {
        List<string> toDelete = new();
        foreach (var (key, value) in _tasks)
        {
            if ((value.TaskCompleted + TimeSpan.FromDays(1)) < DateTime.Now)
                toDelete.Add(key);
        }
        
        toDelete.ForEach(x => _tasks.Remove(x));
    }
}