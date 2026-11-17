using AliyunDdns.Domain;

namespace AliyunDdns.Infrastructure.Ip;

public enum IpSourceFormat
{
    PlainText,
    JsonProperty,
    TextExtraction
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
        // IPv4 先尝试中国区域来源，再回退到全球来源。
        yield return new HttpIpSource
        {
            Name = "ipip-ipv4",
            Uri = new Uri("https://myip.ipip.net/json"),
            Type = DnsRecordType.A,
            Format = IpSourceFormat.JsonProperty,
            JsonPropertyName = "data.ip",
            Region = IpSourceRegion.China
        };
        yield return new HttpIpSource
        {
            Name = "oray-ipv4",
            Uri = new Uri("https://ddns.oray.com/checkip"),
            Type = DnsRecordType.A,
            Format = IpSourceFormat.TextExtraction,
            Region = IpSourceRegion.China
        };
        yield return new HttpIpSource
        {
            Name = "qqnews-ipv4",
            Uri = new Uri("https://i.news.qq.com/api/ip2city"),
            Type = DnsRecordType.A,
            Format = IpSourceFormat.JsonProperty,
            JsonPropertyName = "ip",
            Region = IpSourceRegion.China
        };
        yield return new HttpIpSource
        {
            Name = "yinghualuo-ipv4",
            Uri = new Uri("https://ip.yinghualuo.cn/bejson"),
            Type = DnsRecordType.A,
            Format = IpSourceFormat.JsonProperty,
            JsonPropertyName = "ip",
            Region = IpSourceRegion.China
        };
        yield return new HttpIpSource { Name = "aws-checkip-ipv4", Uri = new Uri("https://checkip.amazonaws.com/"), Type = DnsRecordType.A };
        yield return new HttpIpSource { Name = "icanhazip-ipv4", Uri = new Uri("https://ipv4.icanhazip.com/"), Type = DnsRecordType.A };
        yield return new HttpIpSource { Name = "ip-sb-ipv4", Uri = new Uri("https://api.ip.sb/ip"), Type = DnsRecordType.A };
        yield return new HttpIpSource { Name = "ifconfig-me-ipv4", Uri = new Uri("https://ifconfig.me/ip"), Type = DnsRecordType.A };

        // IPv6 中国区域来源优先，再回退到全球来源。
        yield return new HttpIpSource
        {
            Name = "dnspod-ipv6",
            Uri = new Uri("https://ipv6.ddnspod.com/"),
            Type = DnsRecordType.AAAA,
            Region = IpSourceRegion.China
        };
        yield return new HttpIpSource { Name = "ipify-ipv6", Uri = new Uri("https://api6.ipify.org/"), Type = DnsRecordType.AAAA };
        yield return new HttpIpSource { Name = "icanhazip-ipv6", Uri = new Uri("https://ipv6.icanhazip.com/"), Type = DnsRecordType.AAAA };
        yield return new HttpIpSource { Name = "ident-me-ipv6", Uri = new Uri("https://v6.ident.me/"), Type = DnsRecordType.AAAA };
        yield return new HttpIpSource { Name = "ip-sb-ipv6", Uri = new Uri("https://api-ipv6.ip.sb/ip"), Type = DnsRecordType.AAAA };
    }
}
