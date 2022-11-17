namespace DeckPersonalisationApi.Services.Tasks;

public interface IDirTaskPart : ITaskPart
{
    public string DirPath { get; }
}