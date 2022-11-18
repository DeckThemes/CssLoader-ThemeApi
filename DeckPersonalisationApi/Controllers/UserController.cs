using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Model.Dto.External.GET;
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
    private UserService _user;

    public UserController(UserService service, CssThemeService css, UserService user)
    {
        _service = service;
        _css = css;
        _user = user;
    }

    [HttpGet("{id}/css_themes")]
    public IActionResult GetCssThemes(string id, int page = 1, int perPage = 50, string filters = "", string order = "")
    {
        User? user = _user.GetActiveUserById(id);

        if (user == null)
            return new NotFoundResult();
        
        PaginationDto paginationDto = new(page, perPage, filters, order);
        PaginatedResponse<CssTheme> response = _css.GetUsersThemes(user, paginationDto);
        return new OkObjectResult(response.ToDto());
    }
    
    [HttpGet("{id}/css_themes/filters")]
    public IActionResult GetCssThemesFilters(string id)
    {
        return new OkObjectResult(new PaginationFilters(_css.Targets, _css.Orders().ToList()));
    }
}