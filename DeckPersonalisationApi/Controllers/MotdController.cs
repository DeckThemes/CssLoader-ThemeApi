using DeckPersonalisationApi.Extensions;
using DeckPersonalisationApi.Middleware.JwtRole;
using DeckPersonalisationApi.Model;
using DeckPersonalisationApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeckPersonalisationApi.Controllers;

[ApiController]
[Route("motd")]
public class MotdController(MotdService service) : Controller
{
    [HttpGet]
    public IActionResult Get()
    {
        var motd = service.Get();
        Response.Headers.CacheControl = "public, max-age=86400";
        return motd == null ? new NotFoundResult() : motd.Ok();
    }

    public record CreateMotd(string Name, string Description, MessageOfTheDaySeverity Severity);

    [HttpPost]
    [Authorize]
    [JwtRoleRequire(Permissions.ManageApi)]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult Post(CreateMotd motd)
        => service.Set(motd.Name, motd.Description, motd.Severity).Ok();
    
    [HttpPut]
    [Authorize]
    [JwtRoleRequire(Permissions.ManageApi)]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult Update(CreateMotd motd)
        => service.Update(motd.Name, motd.Description, motd.Severity).Ok();

    [HttpDelete]
    [Authorize]
    [JwtRoleRequire(Permissions.ManageApi)]
    [JwtRoleReject(Permissions.FromApiToken)]
    public IActionResult Delete()
    {
        service.Delete();
        return "".Ok();
    }
}