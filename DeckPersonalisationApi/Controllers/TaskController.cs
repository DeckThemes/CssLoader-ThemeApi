using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Model.Dto.External.GET;
using DeckPersonalisationApi.Services;
using DeckPersonalisationApi.Services.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeckPersonalisationApi.Controllers;

[ApiController]
[Route("tasks")]
public class TaskController : Controller
{
    private TaskService _service;

    public TaskController(TaskService service)
    {
        _service = service;
    }

    [HttpGet("{id}")]
    [Authorize]
    public IActionResult GetTask(string id)
    {
        AppTask? task = _service.GetTask(id);

        if (task == null)
            return new NotFoundResult();

        return new OkObjectResult(new TaskGetDto(task));
    }
/*
    [HttpPost]
    public IActionResult TestTask()
    {
        DelegateTaskPart part1 = new(() => Thread.Sleep(10000), () => { }, "Part 1");
        DelegateTaskPart part2 = new(() => Thread.Sleep(10000), () => { }, "Part 2");
        DelegateTaskPart part3 = new(() => throw new TaskFailureException("test"), () => { }, "Part 3");

        AppTaskFromParts task = new(new List<ITaskPart>()
        {
            part1, part2, part3
        }, "Test task", new() { Id = "a"});

        _service.RegisterTask(task);

        return new OkObjectResult(new TaskIdGetDto(task.Id));
    }
*/
}