using System.ComponentModel.DataAnnotations;
using DeckPersonalisationApi.Exceptions;
using DeckPersonalisationApi.Model;
using Microsoft.AspNetCore.Mvc;

namespace DeckPersonalisationApi.Extensions;

public static class ObjectExtensions
{
    public static T Require<T>(this T? o, string notFoundMessage = "Object not found")
    {
        if (o == null)
            throw new NotFoundException(notFoundMessage);

        return o;
    }

    public static IActionResult Ok(this object o)
        => new OkObjectResult(o);

    public static IActionResult Ok<T>(this T o) where T : IToDto
        => o.ToDtoObject().Ok();
}