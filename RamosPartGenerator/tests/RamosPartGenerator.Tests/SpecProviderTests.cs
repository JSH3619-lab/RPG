using RamosPartGenerator.Core.Services;

namespace RamosPartGenerator.Tests;

public class SpecProviderTests
{
    [Fact]
    public void Load_ReadsSupportedRevisions()
    {
        var specDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "specs"));
        var provider = new SpecProvider(specDirectory);

        provider.Load();

        Assert.Equal(new[] { "30" }, provider.GetSupportedRevisions());
    }

    [Fact]
    public void Load_FallsBackToEmbeddedSpecsWhenExternalFilesAreMissing()
    {
        var missingSpecDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "specs");
        var provider = new SpecProvider(missingSpecDirectory);

        provider.Load();

        Assert.Equal(new[] { "30" }, provider.GetSupportedRevisions());
        Assert.Equal("30", provider.GetRevisionSpec("30").Revision);
    }
}
