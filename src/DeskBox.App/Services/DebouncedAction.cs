namespace DeskBox.App.Services;

public sealed class DebouncedAction
{
    private readonly Action _action;
    private readonly int _delayMs;
    private CancellationTokenSource? _cts;

    public DebouncedAction(Action action, int delayMs = 400)
    {
        _action = action;
        _delayMs = delayMs;
    }

    public void Invoke()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_delayMs, token);
                if (!token.IsCancellationRequested)
                {
                    _action();
                }
            }
            catch (TaskCanceledException)
            {
                // Ignored.
            }
        });
    }
}
