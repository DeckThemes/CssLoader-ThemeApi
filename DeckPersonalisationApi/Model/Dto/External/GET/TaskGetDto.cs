using DeckPersonalisationApi.Services.Tasks;

namespace DeckPersonalisationApi.Model.Dto.External.GET;

public class TaskGetDto
{
    public string Id { get; }
    public string Name { get; }
    public string Status { get; }
    public DateTimeOffset? Started { get; }
    public DateTimeOffset? Completed { get; }
    public bool Success { get; }

    public TaskGetDto(AppTask task)
    {
        Id = task.Id;
        Name = task.Name;
        Status = task.Status;
        Started = task.TaskStarted;
        Completed = task.TaskCompleted;
        Success = task.Success;
    }
}