﻿using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto.External.GET;

namespace DeckPersonalisationApi.Services.Tasks;

public abstract class AppTask : IToDto<TaskGetDto>
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "App Task";
    public string Status { get; set; } = "?";
    public DateTimeOffset? TaskStarted { get; set; }

    public bool Success { get; set; } = false;
    public DateTimeOffset? TaskCompleted { get; set; }

    public event Action<AppTask>? OnStarted;
    public event Action<AppTask>? OnCompleted;

    public void InvokeOnStarted() => OnStarted?.Invoke(this);
    public void InvokeOnCompleted() => OnCompleted?.Invoke(this);
    
    public User Owner { get; private set; }

    public AppTask(User owner)
    {
        Owner = owner;
        
        OnStarted += x =>
        {
            x.TaskStarted = DateTimeOffset.Now;
            x.Status = $"{Name} started";
        };
        OnCompleted += x =>
        {
            x.TaskCompleted = DateTimeOffset.Now;
            if (x.Success)
                x.Status = $"{Name} completed Successfully";
        };
    }

    public abstract void SetupServices(IServiceProvider provider);

    public abstract void Run();

    public TaskGetDto ToDto()
        => new(this);

    public object ToDtoObject()
        => ToDto();
}