namespace DeskBox.App.Services;

public sealed class SingleInstanceService : IDisposable
{
    private readonly Mutex _mutex;
    public bool IsFirstInstance { get; }

    public SingleInstanceService()
    {
        _mutex = new Mutex(true, "DeskBox_SingleInstance_v1", out var createdNew);
        IsFirstInstance = createdNew;
    }

    public void Dispose() => _mutex.Dispose();
}
