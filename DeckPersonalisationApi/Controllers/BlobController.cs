using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Extensions;
using DeckPersonalisationApi.Middleware.JwtRole;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto.External.GET;
using DeckPersonalisationApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp.Formats.Webp;

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

    [HttpGet("{id}/thumb")]
    public IActionResult GetThumbImage(string id, int maxWidth = 0, int maxHeight = 0)
    {
        id = id.Split(".").First();
        SavedBlob? blob = _service.GetBlob(id);

        if (blob == null || blob.Deleted)
            throw new NotFoundException($"Could not find blob with id '{id}'");

        if (!(blob.Type is BlobType.Jpg or BlobType.Png))
            throw new NotFoundException("Blob is not an image");

        if (maxWidth != 0 && maxHeight != 0)
            throw new BadRequestException("Thumbnail cannot both have a max width and height");

        if (maxWidth == 0 && maxHeight == 0)
            maxWidth = 400;

        MemoryStream memoryStream = new();
        
        using (var fileStream = System.IO.File.OpenRead(_service.GetFullFilePath(blob)))
        using (Image image = Image.Load(fileStream))
        {
            bool resizeNeeded = (maxWidth > 0 && image.Width > maxWidth) || (maxHeight > 0 && image.Height > maxHeight);
            if (resizeNeeded)
                image.Mutate(x => x.Resize(maxWidth, maxHeight));
            
            image.Save(memoryStream, new WebpEncoder());
        }

        memoryStream.Seek(0, SeekOrigin.Begin);
        
        return new FileStreamResult(memoryStream, "image/webp");
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
    [JwtRoleReject(Permissions.FromApiToken, true)]
    public IActionResult PostBlob(IFormFile file)
    {
        UserJwtDto dto = _jwt.DecodeToken(Request).Require();
        return _service.CreateBlob(file.OpenReadStream(), file.FileName, dto.Id).Ok();
    }
}