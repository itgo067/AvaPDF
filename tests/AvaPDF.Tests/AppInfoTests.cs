using AvaPDF.Desktop;
using AvaPDF.Desktop.ViewModels;
using Xunit;

namespace AvaPDF.Tests;

public sealed class AppInfoTests
{
    [Fact]
    public void About_uses_the_minver_assembly_informational_version()
    {
        var about = new AboutViewModel();

        Assert.False(string.IsNullOrWhiteSpace(AppInfo.Version));
        Assert.Matches(@"^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$", AppInfo.Version);
        Assert.Equal(AppInfo.Version, about.Version);
        Assert.Equal("https://github.com/itgo067/AvaPDF", AppInfo.GitHubUrl);
    }
}
