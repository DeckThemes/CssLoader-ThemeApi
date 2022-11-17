using System.Diagnostics;
using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Model;

namespace DeckPersonalisationApi.Services.Tasks;

public class AppTaskFromParts : AppTask
{
    private List<ITaskPart> _tasks = new();
    
    public AppTaskFromParts(IEnumerable<ITaskPart> tasks, string name, User owner)
        : base(owner)
    {
        _tasks = tasks.ToList();
        Name = name;
    }

    public override void SetupServices(IServiceProvider provider)
    {
        foreach (var taskPart in _tasks)
        {
            taskPart.SetupServices(provider);
        }
    }

    public override void Run()
    {
        InvokeOnStarted();

        string taskName = "";
        int taskIndex = 0;
        try
        {
            foreach (var task in _tasks)
            {
                taskName = task.Name;
                Status = $"Task {taskIndex + 1}/{_tasks.Count}: {taskName}";
                Console.WriteLine(Status);
                task.Execute();
                taskIndex++;
            }

            taskIndex--;
            Success = true;
        }
        catch (TaskFailureException e)
        {
            Status = $"Failed at task '{taskName}': {e.Message}";
            Success = false;
        }
        catch (Exception e)
        {
            Status = $"Failed at task '{taskName}': Internal Server Error";
            Success = false;
        }

        try
        {
            for (int i = 0; i <= taskIndex; i++)
            {
                _tasks[i].Cleanup(Success);
            }
        }
        catch (Exception _){}

        InvokeOnCompleted();
    }
}