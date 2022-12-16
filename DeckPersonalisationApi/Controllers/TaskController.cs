using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Extensions;
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
        return _service.GetTask(id).Require().Ok();
    }
}