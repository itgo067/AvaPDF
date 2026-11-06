using AvaPDF.Application.Operations;
using AvaPDF.Desktop.Services;
using AvaPDF.Desktop.ViewModels;
using AvaPDF.Domain;
using Xunit;

namespace AvaPDF.Tests;

public sealed class UpdateNotificationTests
{
    [Theory]
    [InlineData("0.1.1-alpha.0.22", "0.2.3", -1)]
    [InlineData("0.2.3", "0.2.3", 0)]
    [InlineData("0.2.3", "0.2.4-alpha.1", -1)]
    [InlineData("0.2.3-rc.1", "0.2.3", -1)]
    [InlineData("0.2.3-alpha.1", "0.2.3-beta.1", -1)]
    [InlineData("0.2.3-alpha.9", "0.2.3-alpha.10", -1)]
    public void Compares_release_and_prerelease_versions(string current, string latest, int expected)
    {
        Assert.Equal(expected, ReleaseVersionComparer.Compare(current, latest));
    }

    [Fact]
    public async Task Returns_update_for_newer_github_release()
    {
        var handler = new StubHttpHandler("{\"tag_name\":\"v9.9.9\",\"html_url\":\"https://example.test/v9.9.9\"}");
        var service = new UpdateService(new HttpClient(handler));

        var update = await service.CheckForUpdateAsync();

        Assert.NotNull(update);
        Assert.Equal("v9.9.9", update!.LatestVersion);
        Assert.Equal("https://example.test/v9.9.9", update.ReleaseUrl);
        Assert.Equal("https://api.github.com/repos/itgo067/AvaPDF/releases/latest", handler.RequestUri);
    }

    [Fact]
    public async Task Displays_update_and_ignores_only_current_release()
    {
        var updates = new FakeUpdateService();
        var viewModel = CreateViewModel(updates);

        await viewModel.CheckForUpdatesAsync();

        Assert.True(viewModel.HasUpdate);
        Assert.Contains("v0.3.0", viewModel.UpdateMessage);
        Assert.Equal("0.2.3", viewModel.AvailableUpdate!.CurrentVersion);

        viewModel.IgnoreUpdate();

        Assert.False(viewModel.HasUpdate);
        Assert.Equal("v0.3.0", updates.IgnoredVersion);
    }

    private static MainWindowViewModel CreateViewModel(IUpdateService updateService) => new(
        new PdfJobService([]),
        new FakeFileDialogService(),
        new FakeShellService(),
        new FakeThemeService(),
        updateService);

    private sealed class StubHttpHandler : HttpMessageHandler
    {
        private readonly string _json;

        public StubHttpHandler(string json) => _json = json;

        public string RequestUri { get; private set; } = string.Empty;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri?.ToString() ?? string.Empty;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(_json, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class FakeUpdateService : IUpdateService
    {
        public string? IgnoredVersion { get; private set; }

        public Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<UpdateInfo?>(new UpdateInfo("0.2.3", "v0.3.0", "https://example.test/release"));

        public void IgnoreVersion(string version) => IgnoredVersion = version;
    }

    private sealed class FakeFileDialogService : IFileDialogService
    {
        public Task<IReadOnlyList<string>> PickFilesAsync(PdfInputKind inputKind, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>([]);

        public Task<string?> PickFolderAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
    }

    private sealed class FakeShellService : IShellService
    {
        public void OpenFolder(string path)
        {
        }

        public void OpenFile(string path)
        {
        }
    }

    private sealed class FakeThemeService : IThemeService
    {
        public AppThemeMode CurrentMode => AppThemeMode.System;

        public void ApplySavedTheme()
        {
        }

        public void SetTheme(AppThemeMode mode)
        {
        }
    }
}
