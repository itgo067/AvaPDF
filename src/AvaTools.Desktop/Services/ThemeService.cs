using Avalonia.Styling;

namespace AvaTools.Desktop.Services;

public enum AppThemeMode
{
    System,
    Light,
    Dark
}

public interface IThemeService
{
    AppThemeMode CurrentMode { get; }

    void ApplySavedTheme();

    void SetTheme(AppThemeMode mode);
}

public sealed class ThemeService : IThemeService
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AvaTools");
    private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "theme.txt");

    public AppThemeMode CurrentMode { get; private set; } = LoadSavedMode();

    public void ApplySavedTheme() => ApplyTheme(CurrentMode);

    public void SetTheme(AppThemeMode mode)
    {
        CurrentMode = mode;
        ApplyTheme(mode);
        SaveMode(mode);
    }

    private static void ApplyTheme(AppThemeMode mode)
    {
        if (Avalonia.Application.Current is null)
        {
            return;
        }

        Avalonia.Application.Current.RequestedThemeVariant = mode switch
        {
            AppThemeMode.Light => ThemeVariant.Light,
            AppThemeMode.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }

    private static AppThemeMode LoadSavedMode()
    {
        try
        {
            return File.Exists(SettingsPath)
                && Enum.TryParse<AppThemeMode>(File.ReadAllText(SettingsPath).Trim(), out var mode)
                ? mode
                : AppThemeMode.System;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return AppThemeMode.System;
        }
    }

    private static void SaveMode(AppThemeMode mode)
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            File.WriteAllText(SettingsPath, mode.ToString());
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Theme switching must remain available when preferences cannot be persisted.
        }
    }
}
