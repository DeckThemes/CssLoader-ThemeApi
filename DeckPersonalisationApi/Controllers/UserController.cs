using DeckPersonalisationApi.Model.Dto.Internal.GET;
using Microsoft.AspNetCore.Mvc;

namespace DeckPersonalisationApi.Controllers;

[ApiController]
[Route("users")]
public class UserController : Controller
{
    [HttpGet("css_themes")]
    public IActionResult GetCssThemes(PaginationDto pagination)
    {
        return new OkResult();
    }
    
    [HttpGet("css_themes/filters")]
    public IActionResult GetCssThemesFilters(PaginationDto pagination)
    {
        return new OkResult();
    }
}