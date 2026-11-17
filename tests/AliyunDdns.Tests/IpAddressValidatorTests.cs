using System.Net;
using AliyunDdns.Domain;
using AliyunDdns.Infrastructure.Ip;
using Xunit;

namespace AliyunDdns.Tests;

public sealed class IpAddressValidatorTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.0.0.1")]
    [InlineData("192.168.1.1")]
    [InlineData("169.254.1.1")]
    [InlineData("100.64.1.1")]
    [InlineData("192.0.2.1")]
    [InlineData("224.0.0.1")]
    [InlineData("not-an-ip")]
    public void InvalidPublicIPv4_IsRejected(string text)
    {
        Assert.False(IpAddressValidator.TryParsePublic(text, DnsRecordType.A, out _));
    }

    [Theory]
    [InlineData("::1")]
    [InlineData("fe80::1")]
    [InlineData("fc00::1")]
    [InlineData("ff02::1")]
    [InlineData("2001:db8::1")]
    [InlineData("::ffff:8.8.8.8")]
    [InlineData("64:ff9b:1::1")]
    [InlineData("100::1")]
    [InlineData("100:0:0:1::1")]
    [InlineData("2001:2::1")]
    [InlineData("2001:10::1")]
    [InlineData("2001:20::1")]
    [InlineData("3fff::1")]
    [InlineData("5f00::1")]
    public void InvalidPublicIPv6_IsRejected(string text)
    {
        Assert.False(IpAddressValidator.TryParsePublic(text, DnsRecordType.AAAA, out _));
    }

    [Theory]
    [InlineData("8.8.8.8", DnsRecordType.A)]
    [InlineData("1.1.1.1", DnsRecordType.A)]
    [InlineData("240e::1", DnsRecordType.AAAA)]
    [InlineData("2606:4700:4700::1111", DnsRecordType.AAAA)]
    [InlineData("64:ff9b::808:808", DnsRecordType.AAAA)]
    [InlineData("2001:3::1", DnsRecordType.AAAA)]
    [InlineData("2001:30::1", DnsRecordType.AAAA)]
    [InlineData("2620:4f:8000::1", DnsRecordType.AAAA)]
    [InlineData("2001:4860:4860::8888", DnsRecordType.AAAA)]
    public void PublicAddress_IsAccepted(string text, DnsRecordType type)
    {
        Assert.True(IpAddressValidator.TryParsePublic(text, type, out var address));
        Assert.Equal(IPAddress.Parse(text), address);
    }

    [Theory]
    [InlineData("169.254.1.1")]
    [InlineData("169.254.255.254")]
    [InlineData("0.0.0.1")]
    [InlineData("0.255.255.254")]
    [InlineData("127.0.0.1")]
    [InlineData("224.0.0.1")]
    [InlineData("fe80::1")]
    public void InvalidLocalAddress_IsRejected(string text)
    {
        Assert.False(IpAddressValidator.IsUsableLocal(IPAddress.Parse(text)));
    }

    [Theory]
    [InlineData("10.0.0.1")]
    [InlineData("172.16.0.1")]
    [InlineData("192.168.1.1")]
    [InlineData("8.8.8.8")]
    [InlineData("fc00::1")]
    [InlineData("240e::1")]
    public void ReasonableLocalAddress_IsAccepted(string text)
    {
        Assert.True(IpAddressValidator.IsUsableLocal(IPAddress.Parse(text)));
    }

    [Fact]
    public void WrongFamily_IsRejected()
    {
        Assert.False(IpAddressValidator.TryParsePublic("8.8.8.8", DnsRecordType.AAAA, out _));
    }
}
