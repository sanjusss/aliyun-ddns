using AliyunDdns.Domain;

namespace AliyunDdns.Configuration;

public enum ConflictPolicy
{
    Skip,
    Replace
}

public enum IpAddressSource
{
    Public,
    Local
}

public enum LocalAddressPolicy
{
    Stable,
    Eui64,
    First
}

public sealed class DdnsTarget
{
    public string Domain { get; set; } = string.Empty;

    public string? RootDomain { get; set; }

    public List<DnsRecordType> Types { get; set; } = [];

    public string Line { get; set; } = "default";

    public long Ttl { get; set; } = 600;

    public IpAddressSource Source { get; set; }

    public string? Interface { get; set; }

    public List<string> Networks { get; set; } = [];

    public bool AllowAnyInterface { get; set; }

    public LocalAddressPolicy AddressPolicy { get; set; }
}
