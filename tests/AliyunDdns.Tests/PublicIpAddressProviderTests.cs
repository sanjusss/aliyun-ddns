using System.Net;
using AliyunDdns.Configuration;
using AliyunDdns.Domain;
using AliyunDdns.Infrastructure.Ip;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AliyunDdns.Tests;

public sealed class PublicIpAddressProviderTests
{
    [Fact]
    public async Task HttpFailure_FallsThroughToNextTrustedSource()
    {
        var calls = 0;
        var handler = new FakeHttpMessageHandler((_, _) =>
        {
            calls++;
            return Task.FromResult(calls == 1 ? HttpResponses.Text("error", HttpStatusCode.InternalServerError) : HttpResponses.Text("8.8.8.8\n"));
        });
        var provider = Provider(handler, Source("first"), Source("second"));
        Assert.Equal(IPAddress.Parse("8.8.8.8"), await provider.GetAddressAsync(TestData.Target(), DnsRecordType.A, CancellationToken.None));
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task PrivateAddress_IsRejectedBeforeNextSource()
    {
        var handler = new FakeHttpMessageHandler((request, _) => Task.FromResult(
            request.RequestUri!.Host == "private.test" ? HttpResponses.Text("127.0.0.1") : HttpResponses.Text("1.1.1.1")));
        var provider = Provider(handler, Source("private", "https://private.test/ip"), Source("valid", "https://valid.test/ip"));
        Assert.Equal(IPAddress.Parse("1.1.1.1"), await provider.GetAddressAsync(TestData.Target(), DnsRecordType.A, CancellationToken.None));
    }

    [Fact]
    public async Task JsonProperty_IsParsedExactly()
    {
        var source = Source("json");
        source = source with { Format = IpSourceFormat.JsonProperty, JsonPropertyName = "ip" };
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(HttpResponses.Text("{\"ip\":\"8.8.4.4\"}")));
        Assert.Equal(IPAddress.Parse("8.8.4.4"), await Provider(handler, source).GetAddressAsync(TestData.Target(), DnsRecordType.A, CancellationToken.None));
    }

    [Fact]
    public async Task OversizedResponse_ReturnsNull()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(HttpResponses.Text(new string('1', 1100))));
        Assert.Null(await Provider(handler, Source("large")).GetAddressAsync(TestData.Target(), DnsRecordType.A, CancellationToken.None));
    }

    [Fact]
    public async Task ChinaOnly_SkipsGlobalSources()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(HttpResponses.Text("8.8.8.8")));
        var options = TestData.Options();
        options.Updater.ChinaIPv4Only = true;
        var provider = Provider(handler, options, Source("global"), Source("china", "https://china.test/ip") with { Region = IpSourceRegion.China });
        await provider.GetAddressAsync(TestData.Target(), DnsRecordType.A, CancellationToken.None);
        Assert.Equal("china.test", Assert.Single(handler.Requests).RequestUri!.Host);
    }

    [Fact]
    public async Task Cancellation_IsPropagated()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(HttpResponses.Text("8.8.8.8")));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Provider(handler, Source("source")).GetAddressAsync(TestData.Target(), DnsRecordType.A, cancellation.Token));
    }

    [Fact]
    public void HttpSource_IsRejectedByCatalog()
    {
        Assert.Throws<ArgumentException>(() => new IpSourceCatalog([Source("insecure", "http://insecure.test/ip")]));
    }

    private static PublicIpAddressProvider Provider(FakeHttpMessageHandler handler, params HttpIpSource[] sources)
    {
        return Provider(handler, TestData.Options(), sources);
    }

    private static PublicIpAddressProvider Provider(FakeHttpMessageHandler handler, DdnsOptions options, params HttpIpSource[] sources)
    {
        return new PublicIpAddressProvider(new FakeHttpClientFactory(handler), new IpSourceCatalog(sources),
            Options.Create(options), NullLogger<PublicIpAddressProvider>.Instance);
    }

    private static HttpIpSource Source(string name, string uri = "https://unit.test/ip")
    {
        return new HttpIpSource { Name = name, Uri = new Uri(uri), Type = DnsRecordType.A };
    }
}
