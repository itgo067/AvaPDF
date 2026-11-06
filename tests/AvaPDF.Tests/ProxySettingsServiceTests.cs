using AvaPDF.Desktop.Services;
using Xunit;

namespace AvaPDF.Tests;

public sealed class ProxySettingsServiceTests
{
    [Fact]
    public void Custom_proxy_is_applied_and_persisted()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var settings = new ProxySettingsService(directory.FullName);
            settings.Set(ProxyMode.Custom, "http://127.0.0.1:7890");

            var handler = settings.CreateHandler();
            var reloaded = new ProxySettingsService(directory.FullName);

            Assert.True(handler.UseProxy);
            Assert.Equal("http://127.0.0.1:7890/", handler.Proxy!.GetProxy(new Uri("https://github.com"))!.ToString());
            Assert.Equal(ProxyMode.Custom, reloaded.Mode);
            Assert.Equal("http://127.0.0.1:7890", reloaded.Address);
        }
        finally
        {
            Directory.Delete(directory.FullName, true);
        }
    }

    [Fact]
    public void Disabled_proxy_bypasses_proxy_and_credentials_are_rejected()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var settings = new ProxySettingsService(directory.FullName);
            settings.Set(ProxyMode.Disabled, string.Empty);
            Assert.False(settings.CreateHandler().UseProxy);
            Assert.Throws<ArgumentException>(() => settings.Set(ProxyMode.Custom, "http://user:pass@127.0.0.1:7890"));
        }
        finally
        {
            Directory.Delete(directory.FullName, true);
        }
    }
}
