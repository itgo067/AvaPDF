using System.Net;

namespace AvaPDF.Desktop.Services;

public enum ProxyMode
{
    System,
    Disabled,
    Custom
}

public interface IProxySettingsService
{
    ProxyMode Mode { get; }

    string Address { get; }

    void Set(ProxyMode mode, string address);

    HttpClientHandler CreateHandler();
}

public sealed class ProxySettingsService : IProxySettingsService
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AvaPDF");
    private readonly string _modePath;
    private readonly string _addressPath;

    public ProxySettingsService(string? settingsDirectory = null)
    {
        var directory = settingsDirectory ?? SettingsDirectory;
        _modePath = Path.Combine(directory, "proxy-mode.txt");
        _addressPath = Path.Combine(directory, "proxy-address.txt");
        Mode = LoadMode();
        Address = LoadAddress();
    }

    public ProxyMode Mode { get; private set; }

    public string Address { get; private set; } = string.Empty;

    public void Set(ProxyMode mode, string address)
    {
        address = address.Trim();
        if (mode == ProxyMode.Custom)
        {
            if (!Uri.TryCreate(address, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                || string.IsNullOrWhiteSpace(uri.Host))
            {
                throw new ArgumentException("代理地址必须是有效的 HTTP 或 HTTPS 地址，例如 http://127.0.0.1:7890。", nameof(address));
            }
            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                throw new ArgumentException("代理地址不能包含账号或密码。", nameof(address));
            }
        }

        Mode = mode;
        Address = address;
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            File.WriteAllText(_modePath, mode.ToString());
            File.WriteAllText(_addressPath, address);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Proxy changes still apply to this process when settings cannot be persisted.
        }
    }

    public HttpClientHandler CreateHandler()
    {
        if (Mode == ProxyMode.Custom)
        {
            return new HttpClientHandler
            {
                UseProxy = true,
                Proxy = new WebProxy(Address)
            };
        }
        if (Mode == ProxyMode.Disabled)
        {
            return new HttpClientHandler { UseProxy = false };
        }
        return new HttpClientHandler
        {
            UseProxy = true,
            Proxy = WebRequest.DefaultWebProxy
        };
    }

    private ProxyMode LoadMode()
    {
        try
        {
            return File.Exists(_modePath) && Enum.TryParse<ProxyMode>(File.ReadAllText(_modePath).Trim(), out var mode)
                ? mode
                : ProxyMode.System;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return ProxyMode.System;
        }
    }

    private string LoadAddress()
    {
        try
        {
            return File.Exists(_addressPath) ? File.ReadAllText(_addressPath).Trim() : string.Empty;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return string.Empty;
        }
    }
}
