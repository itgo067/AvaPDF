using AvaPDF.Desktop.Services;
using Xunit;

namespace AvaPDF.Tests;

public sealed class TrayWindowServiceTests
{
    [Fact]
    public void Close_is_cancelled_and_window_hidden_until_explicit_exit()
    {
        var shown = 0;
        var hidden = 0;
        var service = new TrayWindowService(() => shown++, () => hidden++, () => { });

        Assert.True(service.ShouldCancelClose);
        service.Hide();
        service.Show();

        Assert.Equal(1, shown);
        Assert.Equal(1, hidden);
    }

    [Fact]
    public void Explicit_exit_allows_shutdown_and_cannot_be_cancelled()
    {
        var shutdown = false;
        var service = new TrayWindowService(() => { }, () => { }, () => shutdown = true);

        service.Exit();

        Assert.True(shutdown);
        Assert.True(service.IsExitRequested);
        Assert.False(service.ShouldCancelClose);
    }
}
