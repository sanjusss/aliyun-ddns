using System.Reflection;
using Xunit;

namespace AliyunDdns.Tests;

public sealed class VersionTests
{
    [Fact]
    public void DefaultVersion_Is_0_3_0()
    {
        var version = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        Assert.Equal("0.3.0", version);
    }
}
