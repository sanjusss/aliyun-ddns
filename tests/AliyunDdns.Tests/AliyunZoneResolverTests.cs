using AliyunDdns.Infrastructure.Aliyun;
using Xunit;

namespace AliyunDdns.Tests;

public sealed class AliyunZoneResolverTests
{
    [Fact]
    public async Task UsesLongestManagedSuffix()
    {
        var dns = new FakeDnsClient { ManagedDomains = ["com.cn", "example.com", "example.com.cn"] };
        var resolver = new AliyunZoneResolver(dns);
        var target = TestData.Target("ddns.example.com.cn");
        target.RootDomain = null;
        var result = await resolver.ResolveAsync(target, CancellationToken.None);
        Assert.Equal("example.com.cn", result.ZoneName);
        Assert.Equal("ddns", result.HostRecord);
    }

    [Fact]
    public async Task ExplicitRootDomain_DoesNotQueryCatalog()
    {
        var dns = new FakeDnsClient();
        var result = await new AliyunZoneResolver(dns).ResolveAsync(TestData.Target(), CancellationToken.None);
        Assert.Equal("example.com", result.ZoneName);
        Assert.Equal("home", result.HostRecord);
        Assert.Equal(0, dns.GetManagedDomainsCallCount);
    }

    [Fact]
    public async Task ManagedDomains_AreCached()
    {
        var dns = new FakeDnsClient();
        var resolver = new AliyunZoneResolver(dns);
        var target = TestData.Target();
        target.RootDomain = null;
        await resolver.ResolveAsync(target, CancellationToken.None);
        await resolver.ResolveAsync(target, CancellationToken.None);
        Assert.Equal(1, dns.GetManagedDomainsCallCount);
    }

    [Fact]
    public async Task TrailingDotAndCase_AreNormalizedInResolvedDomain()
    {
        var resolver = new AliyunZoneResolver(new FakeDnsClient());
        var result = await resolver.ResolveAsync(TestData.Target("Home.Example.COM."), CancellationToken.None);
        Assert.Equal("home.example.com", result.Domain);
        Assert.Equal("home", result.HostRecord);
    }

    [Fact]
    public async Task WildcardTarget_UsesNormalizedWildcardHostRecord()
    {
        var resolver = new AliyunZoneResolver(new FakeDnsClient());
        var result = await resolver.ResolveAsync(TestData.Target("*.Example.COM."), CancellationToken.None);
        Assert.Equal("*.example.com", result.Domain);
        Assert.Equal("*", result.HostRecord);
    }

    [Fact]
    public async Task MissingManagedZone_Throws()
    {
        var resolver = new AliyunZoneResolver(new FakeDnsClient { ManagedDomains = ["other.example"] });
        var target = TestData.Target();
        target.RootDomain = null;
        await Assert.ThrowsAsync<InvalidOperationException>(() => resolver.ResolveAsync(target, CancellationToken.None));
    }
}
