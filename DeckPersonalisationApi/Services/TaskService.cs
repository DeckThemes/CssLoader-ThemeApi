using DeckPersonalisationApi.Services.Tasks;

namespace DeckPersonalisationApi.Services;

public class TaskService
{
    private Dictionary<string, AppTask> _tasks = new();
    private IServiceProvider _services;

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
    
    // TODO: Throw out tasks that are more than a few hours old
}