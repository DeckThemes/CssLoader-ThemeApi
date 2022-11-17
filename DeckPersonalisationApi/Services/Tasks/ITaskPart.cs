namespace DeckPersonalisationApi.Services.Tasks;

public interface ITaskPart
{
    public string Name { get; }
    public void Execute();
    public void Cleanup(bool success);

    public void SetupServices(IServiceProvider provider)
    {
    }
}