using AliyunDdns.Domain;

namespace AliyunDdns.Infrastructure.Ip;

public enum IpSourceFormat
{
    PlainText,
    JsonProperty
}

public enum IpSourceRegion
{
    Global,
    China
}

public sealed record HttpIpSource
{
    public required string Name { get; init; }

    public required Uri Uri { get; init; }

    public required DnsRecordType Type { get; init; }

    public IpSourceFormat Format { get; init; }

    public string? JsonPropertyName { get; init; }

    public IpSourceRegion Region { get; init; }
}

public sealed class IpSourceCatalog
{
    public IpSourceCatalog(IEnumerable<HttpIpSource>? sources = null)
    {
        var configuredSources = sources?.ToArray();
        Sources = configuredSources is { Length: > 0 } ? configuredSources : DefaultSources().ToArray();
        if (Sources.Any(source => source.Uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("公网 IP 来源必须使用 HTTPS。", nameof(sources));
        }
    }

    public IReadOnlyList<HttpIpSource> Sources { get; }

    private static IEnumerable<HttpIpSource> DefaultSources()
    {
        yield return new HttpIpSource { Name = "ipify-ipv4", Uri = new Uri("https://api.ipify.org/"), Type = DnsRecordType.A };
        yield return new HttpIpSource { Name = "icanhazip-ipv4", Uri = new Uri("https://ipv4.icanhazip.com/"), Type = DnsRecordType.A };
        yield return new HttpIpSource { Name = "ipw-cn-ipv4", Uri = new Uri("https://4.ipw.cn/"), Type = DnsRecordType.A, Region = IpSourceRegion.China };
        yield return new HttpIpSource { Name = "ipify-ipv6", Uri = new Uri("https://api6.ipify.org/"), Type = DnsRecordType.AAAA };
        yield return new HttpIpSource { Name = "icanhazip-ipv6", Uri = new Uri("https://ipv6.icanhazip.com/"), Type = DnsRecordType.AAAA };
        yield return new HttpIpSource { Name = "ipw-cn-ipv6", Uri = new Uri("https://6.ipw.cn/"), Type = DnsRecordType.AAAA, Region = IpSourceRegion.China };
    }
}
