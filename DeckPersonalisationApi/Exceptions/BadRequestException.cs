namespace DeckPersonalisationApi.Exceptions;

public class BadRequestException : Exception, IHttpException
{
    public int StatusCode => StatusCodes.Status400BadRequest;

    public BadRequestException(string message) : base(message)
    {
    }
}