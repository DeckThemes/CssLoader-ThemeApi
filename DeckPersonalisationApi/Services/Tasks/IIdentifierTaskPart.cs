namespace DeckPersonalisationApi.Services.Tasks;

public interface IIdentifierTaskPart : ITaskPart
{
    public string Identifier { get; }
}