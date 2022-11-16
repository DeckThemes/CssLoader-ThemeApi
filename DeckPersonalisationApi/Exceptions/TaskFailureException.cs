namespace DeckPersonalisationApi.Exceptions;

public class TaskFailureException : Exception
{
    public TaskFailureException(string message)
        : base(message)
    {
    }
}