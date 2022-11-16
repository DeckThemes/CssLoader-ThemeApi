namespace DeckPersonalisationApi.Services.Tasks;

public class DelegateTaskPart : ITaskPart
{
    public string Name { get; set; }
    private Action _executeDelegate;
    private Action _cleanupDelegate;

    public DelegateTaskPart(Action executeDelegate, Action cleanupDelegate, string name)
    {
        _executeDelegate = executeDelegate;
        _cleanupDelegate = cleanupDelegate;
        Name = name;
    }

    public void Execute() => _executeDelegate();
    public void Cleanup(bool success) => _cleanupDelegate();
}