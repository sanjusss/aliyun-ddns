using AliyunDdns.Hosting;
using AliyunDdns.Infrastructure.Ip;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AliyunDdns.Tests;

public sealed class ServiceRegistrationTests
{
    [Fact]
    public void EmptySourceCollection_UsesDefaultSources()
    {
        var catalog = new IpSourceCatalog(Array.Empty<HttpIpSource>());
        Assert.NotEmpty(catalog.Sources);
    }

    [Fact]
    public void ProductionRegistration_ResolvesDefaultSourceCatalog()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAliyunDdnsServices(TestData.Options());
        using var provider = services.BuildServiceProvider();
        Assert.NotEmpty(provider.GetRequiredService<IpSourceCatalog>().Sources);
    }
}
