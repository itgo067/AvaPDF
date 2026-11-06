using Velopack;
using Velopack.Sources;

namespace AvaPDF.Desktop.Services;

public interface IVelopackUpdateService
{
    Task ApplyUpdatesAndRestartAsync(Action<int>? progress = null, CancellationToken cancellationToken = default);
}

public sealed class VelopackUpdateService(IProxySettingsService proxySettings) : IVelopackUpdateService
{
    public async Task ApplyUpdatesAndRestartAsync(Action<int>? progress = null, CancellationToken cancellationToken = default)
    {
        var downloader = new ProxyHttpClientFileDownloader(proxySettings);
        var source = new GithubSource(AppInfo.GitHubUrl, null, false, downloader);
        var manager = new UpdateManager(source);
        var updates = await manager.CheckForUpdatesAsync();
        if (updates is null)
        {
            throw new InvalidOperationException("当前已经是最新版本。");
        }

        await manager.DownloadUpdatesAsync(updates, progress, cancellationToken);
        manager.ApplyUpdatesAndRestart(updates.TargetFullRelease);
    }
}

internal sealed class ProxyHttpClientFileDownloader(IProxySettingsService proxySettings) : HttpClientFileDownloader
{
    protected override HttpClientHandler CreateHttpClientHandler() => proxySettings.CreateHandler();
}
