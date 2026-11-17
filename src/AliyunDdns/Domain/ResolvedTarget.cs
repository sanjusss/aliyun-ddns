using AliyunDdns.Configuration;

namespace AliyunDdns.Domain;

public sealed record ResolvedTarget
{
    public required DdnsTarget Target { get; init; }

    public required string ZoneName { get; init; }

    public required string HostRecord { get; init; }

    public required string Domain { get; init; }
}
