using System.ComponentModel.DataAnnotations;
using DeckPersonalisationApi.Exceptions;

namespace DeckPersonalisationApi.Extensions;

public static class ObjectExtensions
{
    public static T Require<T>(this T? o, string notFoundMessage = "Object not found")
    {
        if (o == null)
            throw new NotFoundException("Object not found");

        return o;
    }
}