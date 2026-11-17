using System.Net;
using System.Net.NetworkInformation;
using AliyunDdns.Configuration;
using AliyunDdns.Domain;
using AliyunDdns.Infrastructure.Ip;
using Xunit;

namespace AliyunDdns.Tests;

public sealed class LocalIpAddressProviderTests
{
    [Fact]
    public async Task StablePolicy_PrefersManualAndDeterministicOrder()
    {
        var provider = new LocalIpAddressProvider(new FakeLocalAddressSource([
            Candidate("240e::200", "eth1", SuffixOrigin.Random, temporary: true),
            Candidate("240e::100", "eth0", SuffixOrigin.Manual)
        ]));
        var target = LocalTarget(LocalAddressPolicy.Stable);
        Assert.Equal(IPAddress.Parse("240e::100"), await provider.GetAddressAsync(target, DnsRecordType.AAAA, CancellationToken.None));
    }

    [Fact]
    public async Task Eui64Policy_PrefersEui64Address()
    {
        var provider = new LocalIpAddressProvider(new FakeLocalAddressSource([
            Candidate("240e::100", "eth0", SuffixOrigin.Manual),
            Candidate("240e:0:0:0:211:22ff:fe33:4455", "eth0", SuffixOrigin.LinkLayerAddress)
        ]));
        var target = LocalTarget(LocalAddressPolicy.Eui64);
        Assert.Equal(IPAddress.Parse("240e:0:0:0:211:22ff:fe33:4455"), await provider.GetAddressAsync(target, DnsRecordType.AAAA, CancellationToken.None));
    }

    [Fact]
    public async Task InterfaceAndNetwork_AreFiltered()
    {
        var provider = new LocalIpAddressProvider(new FakeLocalAddressSource([
            Candidate("240e::100", "eth0"),
            Candidate("240e::101", "eth1"),
            Candidate("2001:4860:4860::8888", "eth1")
        ]));
        var target = LocalTarget(LocalAddressPolicy.First);
        target.Interface = "eth1";
        target.Networks = ["240e::/16"];
        Assert.Equal(IPAddress.Parse("240e::101"), await provider.GetAddressAsync(target, DnsRecordType.AAAA, CancellationToken.None));
    }

    [Fact]
    public async Task DeprecatedAndLinkLocal_AreRejected()
    {
        var provider = new LocalIpAddressProvider(new FakeLocalAddressSource([
            Candidate("fe80::1"),
            Candidate("240e::100", deprecated: true)
        ]));
        Assert.Null(await provider.GetAddressAsync(LocalTarget(LocalAddressPolicy.Stable), DnsRecordType.AAAA, CancellationToken.None));
    }

    [Fact]
    public async Task Ipv4LinkLocalAndZeroNetwork_AreNotSelected()
    {
        var provider = new LocalIpAddressProvider(new FakeLocalAddressSource([
            Candidate("169.254.10.20"),
            Candidate("0.0.0.10"),
            Candidate("192.168.1.20")
        ]));
        var target = TestData.Target(source: IpAddressSource.Local);
        target.Types = [DnsRecordType.A];
        target.AddressPolicy = LocalAddressPolicy.First;
        target.Networks = ["192.168.1.0/24"];
        Assert.Equal(IPAddress.Parse("192.168.1.20"),
            await provider.GetAddressAsync(target, DnsRecordType.A, CancellationToken.None));
    }

    [Fact]
    public async Task CommonContainerBridges_AreExcludedWithoutFilters()
    {
        var provider = new LocalIpAddressProvider(new FakeLocalAddressSource([
            Candidate("240e::100", "docker0"),
            Candidate("240e::101", "docker-extra"),
            Candidate("240e::102", "br-lan"),
            Candidate("240e::103", "veth1234"),
            Candidate("240e::104", "vbr0"),
            Candidate("240e::105", "virbr0"),
            Candidate("240e::106", "eth0")
        ]));
        Assert.Equal(IPAddress.Parse("240e::106"),
            await provider.GetAddressAsync(LocalTarget(LocalAddressPolicy.First), DnsRecordType.AAAA, CancellationToken.None));
    }

    [Theory]
    [InlineData("eth0")]
    [InlineData("ens160")]
    [InlineData("tun0")]
    [InlineData("tap0")]
    [InlineData("wg0")]
    [InlineData("ppp0")]
    public async Task OrdinaryAndVpnInterfaces_AreRetained(string interfaceName)
    {
        var provider = new LocalIpAddressProvider(new FakeLocalAddressSource([
            Candidate("240e::200", interfaceName)
        ]));
        Assert.Equal(IPAddress.Parse("240e::200"),
            await provider.GetAddressAsync(LocalTarget(LocalAddressPolicy.First), DnsRecordType.AAAA, CancellationToken.None));
    }

    [Fact]
    public async Task AllowAnyInterface_BypassesAutomaticNameFilter()
    {
        var provider = new LocalIpAddressProvider(new FakeLocalAddressSource([
            Candidate("240e::100", "docker0"),
            Candidate("240e::106", "eth0")
        ]));
        var target = LocalTarget(LocalAddressPolicy.First);
        target.AllowAnyInterface = true;
        Assert.Equal(IPAddress.Parse("240e::100"),
            await provider.GetAddressAsync(target, DnsRecordType.AAAA, CancellationToken.None));
    }

    private static DdnsTarget LocalTarget(LocalAddressPolicy policy)
    {
        var target = TestData.Target(source: IpAddressSource.Local);
        target.Types = [DnsRecordType.AAAA];
        target.AddressPolicy = policy;
        return target;
    }

    private static LocalAddressCandidate Candidate(
        string address,
        string interfaceName = "eth0",
        SuffixOrigin origin = SuffixOrigin.Other,
        bool temporary = false,
        bool deprecated = false)
    {
        return new LocalAddressCandidate
        {
            Address = IPAddress.Parse(address),
            InterfaceName = interfaceName,
            SuffixOrigin = origin,
            IsTemporary = temporary,
            IsDeprecated = deprecated
        };
    }
}
