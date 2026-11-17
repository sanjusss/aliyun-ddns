namespace AliyunDdns.Domain;

public sealed record DnsRecord
{
    public required string RecordId { get; init; }

    public required string DomainName { get; init; }

    public required string HostRecord { get; init; }

    public required DnsRecordType Type { get; init; }

    public required string Value { get; init; }

    public long Ttl { get; init; }

    public string Line { get; init; } = "default";
}
