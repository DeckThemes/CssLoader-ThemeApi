using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Extensions;
using DeckPersonalisationApi.Middleware.JwtRole;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto.External.GET;
using DeckPersonalisationApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeckPersonalisationApi.Controllers;

[ApiController]
[Route("blobs")]
public class BlobController : Controller
{
    private BlobService _service;
    private JwtService _jwt;
    private UserService _user;
    private TaskService _tasks;

    public BlobController(BlobService service, JwtService jwt, UserService user, TaskService tasks)
    {
        _service = service;
        _jwt = jwt;
        _user = user;
        _tasks = tasks;
    }

    [HttpGet("{id}")]
    public IActionResult GetBlob(string id)
    {
        id = id.Split(".").First();
        SavedBlob? image = _service.GetBlob(id);

        if (image == null || image.Deleted)
            throw new NotFoundException($"Could not find blob with id '{id}'");

        string path = _service.GetFullFilePath(image);
        Stream stream = System.IO.File.OpenRead(path);
        _tasks.RegisterDownload(id);
        return new FileStreamResult(stream, image.Type.GetContentType());
    }

    [HttpGet]
    [Authorize]
    public IActionResult GetBlobsFromUser()
    {
        UserJwtDto dto = _jwt.DecodeToken(Request).Require("Decoding JWT failed");
        User user = _user.GetActiveUserById(dto.Id).Require("User does not exist");
        return _service.GetBlobsByUser(user).Select(x => x.Id).ToList().Ok();
    }

    [HttpPost]
    [Authorize]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult PostBlob(IFormFile file)
    {
        UserJwtDto dto = _jwt.DecodeToken(Request).Require();
        return _service.CreateBlob(file.OpenReadStream(), file.FileName, dto.Id).Ok();
    }
}