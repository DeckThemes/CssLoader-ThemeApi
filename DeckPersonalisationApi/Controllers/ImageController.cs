using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto.External.GET;
using DeckPersonalisationApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeckPersonalisationApi.Controllers;

[ApiController]
[Route("image")]
public class ImageController : Controller
{
    private ImageService _service;
    private JwtService _jwt;
    

    public ImageController(ImageService service, JwtService jwt)
    {
        _service = service;
        _jwt = jwt;
    }

    [HttpGet("{id}")]
    public IActionResult GetImage(string id)
    {
        SavedImage? image = _service.GetImage(id);

        if (image == null)
            return new NotFoundResult();

        string path = _service.GetFullFilePath(image);
        Stream stream = System.IO.File.OpenRead(path);
        return new FileStreamResult(stream, "image/jpg");
    }

    [HttpPost]
    [Authorize]
    public IActionResult PostImage(IFormFile file)
    {
        UserJwtDto? dto = _jwt.DecodeToken(Request);
        
        if (dto == null)
            return new BadRequestResult();
        
        dto.RejectPermission(Permissions.FromApiToken);

        return new OkObjectResult(
            new TokenGetDto(_service.CreateImage(file.OpenReadStream(), file.FileName, dto.Id).Id));
    }
}