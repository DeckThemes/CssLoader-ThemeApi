namespace DeckPersonalisationApi.Exceptions;

public class NotFoundException : Exception, IHttpException
{
    public int StatusCode => StatusCodes.Status404NotFound;

    public NotFoundException(string message) : base(message)
    { }
}