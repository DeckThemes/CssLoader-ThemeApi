namespace DeckPersonalisationApi.Services.Tasks;

public interface IFullPathTaskPart : ITaskPart
{
    public string FullPath { get; }
}