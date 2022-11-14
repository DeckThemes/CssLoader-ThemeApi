namespace DeckPersonalisationApi.Exceptions;

public interface IHttpException
{
    public int StatusCode { get; }
    public string Message { get; }
}