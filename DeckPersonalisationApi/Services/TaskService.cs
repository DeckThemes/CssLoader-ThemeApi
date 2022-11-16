using DeckPersonalisationApi.Services.Tasks;

namespace DeckPersonalisationApi.Services;

public class TaskService
{
    private Dictionary<string, AppTask> _tasks = new();

    public AppTask? GetTask(string id)
        => _tasks.ContainsKey(id) ? _tasks[id] : null;

    public string RegisterTask(AppTask task)
    {
        task.Id = Guid.NewGuid().ToString();
        _tasks.Add(task.Id, task);
        Thread thread = new(task.Run);
        thread.Start();
        return task.Id;
    }
    
    // TODO: Throw out tasks that are more than a few hours old
}