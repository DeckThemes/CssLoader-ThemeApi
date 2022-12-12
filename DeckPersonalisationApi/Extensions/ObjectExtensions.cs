using System.ComponentModel.DataAnnotations;
using DeckPersonalisationApi.Exceptions;
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

    public static IActionResult Ok<T>(this T o)
        => new OkObjectResult(o);
}