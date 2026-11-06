using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AvaPDF.Desktop.Services;

public sealed record UpdateInfo(
    string CurrentVersion,
    string LatestVersion,
    string ReleaseUrl);

public interface IUpdateService
{
    Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default);

    void IgnoreVersion(string version);
}

public sealed class UpdateService : IUpdateService
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/itgo067/AvaPDF/releases/latest";
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AvaPDF");
    private static readonly string IgnoredVersionPath = Path.Combine(SettingsDirectory, "ignored-version.txt");
    private readonly HttpClient _httpClient;

    public UpdateService()
        : this(new ProxySettingsService())
    {
    }

    public UpdateService(IProxySettingsService proxySettings)
        : this(new HttpClient(proxySettings.CreateHandler()))
    {
    }

    public UpdateService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"AvaPDF/{AppInfo.Version}");
        _httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    }

    public async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(8));
            var release = await _httpClient.GetFromJsonAsync(
                LatestReleaseUrl,
                UpdateReleaseJsonContext.Default.GitHubRelease,
                timeout.Token);
            if (release is null || string.IsNullOrWhiteSpace(release.TagName))
            {
                return null;
            }

            var latest = release.TagName;
            if (ReleaseVersionComparer.Compare(AppInfo.Version, latest) >= 0
                || string.Equals(LoadIgnoredVersion(), latest, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            return new UpdateInfo(AppInfo.Version, latest, release.HtmlUrl);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            return null;
        }
    }

    public void IgnoreVersion(string version)
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            File.WriteAllText(IgnoredVersionPath, version);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Ignore-version persistence is optional; never block the application.
        }
    }

    private static string? LoadIgnoredVersion()
    {
        try
        {
            return File.Exists(IgnoredVersionPath) ? File.ReadAllText(IgnoredVersionPath).Trim() : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}

public static class ReleaseVersionComparer
{
    public static int Compare(string current, string latest)
    {
        var left = ReleaseVersion.Parse(current);
        var right = ReleaseVersion.Parse(latest);
        var numeric = left.BaseVersion.CompareTo(right.BaseVersion);
        if (numeric != 0)
        {
            return numeric;
        }
        if (left.PreRelease is null && right.PreRelease is not null) return 1;
        if (left.PreRelease is not null && right.PreRelease is null) return -1;
        if (left.PreRelease is null || right.PreRelease is null) return 0;

        var leftParts = left.PreRelease.Split('.');
        var rightParts = right.PreRelease.Split('.');
        var channel = ChannelRank(leftParts[0]).CompareTo(ChannelRank(rightParts[0]));
        if (channel != 0) return channel;
        var leftNumber = NumericSuffix(leftParts);
        var rightNumber = NumericSuffix(rightParts);
        return leftNumber.CompareTo(rightNumber);
    }

    private static int ChannelRank(string value) => value.ToLowerInvariant() switch
    {
        "dev" => 0,
        "alpha" => 1,
        "beta" => 2,
        "rc" => 3,
        _ => 4
    };

    private static long NumericSuffix(string[] parts)
    {
        var digits = string.Concat(parts.Skip(1).Select(part => part.Length > 0 && char.IsDigit(part[0]) ? part : "0"));
        return long.TryParse(digits, out var number) ? number : 0;
    }
}

public readonly record struct ReleaseVersion(Version BaseVersion, string? PreRelease)
{
    public static ReleaseVersion Parse(string value)
    {
        var normalized = value.Trim().TrimStart('v', 'V');
        var separator = normalized.IndexOf('-');
        var baseText = separator >= 0 ? normalized[..separator] : normalized;
        var suffix = separator >= 0 ? normalized[(separator + 1)..] : null;
        return new ReleaseVersion(Version.Parse(baseText), suffix);
    }
}

public sealed record GitHubRelease(
    [property: JsonPropertyName("tag_name")] string TagName,
    [property: JsonPropertyName("html_url")] string HtmlUrl);

[JsonSerializable(typeof(GitHubRelease))]
internal sealed partial class UpdateReleaseJsonContext : JsonSerializerContext;
