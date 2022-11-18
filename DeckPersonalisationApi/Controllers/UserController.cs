using DeckPersonalisationApi.Model.Dto.Internal.GET;
using DeckPersonalisationApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace DeckPersonalisationApi.Controllers;

[ApiController]
[Route("users")]
public class UserController : Controller
{
    private UserService _service;
    private CssThemeService _css;

    public UserController(UserService service, CssThemeService css)
    {
        _service = service;
        _css = css;
    }

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