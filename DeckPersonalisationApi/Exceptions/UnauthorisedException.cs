namespace DeckPersonalisationApi.Exceptions;

public class UnauthorisedException : Exception, IHttpException
{
    public int StatusCode => StatusCodes.Status401Unauthorized;

    public UnauthorisedException(string message)
        : base(message)
    {
    }
}