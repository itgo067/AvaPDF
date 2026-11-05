namespace AvaPDF.Desktop.Services;

public sealed class TrayWindowService
{
    private readonly Action _show;
    private readonly Action _hide;
    private readonly Action _shutdown;

    public TrayWindowService(Action show, Action hide, Action shutdown)
    {
        _show = show;
        _hide = hide;
        _shutdown = shutdown;
    }

    public bool IsExitRequested { get; private set; }

    public bool ShouldCancelClose => !IsExitRequested;

    public void Show() => _show();

    public void Hide() => _hide();

    public void Exit()
    {
        IsExitRequested = true;
        _shutdown();
    }
}
