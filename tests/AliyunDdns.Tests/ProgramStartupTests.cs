using AliyunDdns;
using Xunit;

namespace AliyunDdns.Tests;

public sealed class ProgramStartupTests
{
    [Fact]
    public async Task InvalidJson_ReturnsConfigurationExitCode()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "{ invalid json");
            Assert.Equal(2, await Program.Main(["--config", path, "--validate-config"]));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task UnknownArgument_ReturnsConfigurationExitCode()
    {
        Assert.Equal(2, await Program.Main(["--unknown-option"]));
    }
}
