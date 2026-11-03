using AvaPDF.Desktop;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace AvaPDF.Tests;

public sealed class SemiThemeResourceTests
{
    private static readonly string[] RequiredKeys =
    [
        "SemiColorBackground0",
        "SemiColorBackground1",
        "SemiColorBorder",
        "SemiColorText0",
        "SemiColorText2",
        "SemiColorPrimary",
        "SemiColorPrimaryLight",
        "SemiColorSuccess",
        "SemiColorSuccessLight",
        "SemiColorDanger",
        "SemiColorDangerLight",
        "SemiColorWarning",
        "SemiColorWarningLight",
        "SemiColorFill0"
    ];

    [Fact]
    public void Semi_palette_contains_all_application_colors_in_light_and_dark()
    {
        var app = new App();
        app.Initialize();

        foreach (var variant in new[] { ThemeVariant.Light, ThemeVariant.Dark })
        {
            foreach (var key in RequiredKeys)
            {
                Assert.True(app.TryGetResource(key, variant, out var value), $"Missing {key} in {variant}");
                Assert.IsAssignableFrom<SolidColorBrush>(value);
            }
        }
    }
}
