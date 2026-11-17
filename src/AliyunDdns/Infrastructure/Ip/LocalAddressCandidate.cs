using System.Net;
using System.Net.NetworkInformation;

namespace AliyunDdns.Infrastructure.Ip;

public sealed record LocalAddressCandidate
{
    public required string InterfaceName { get; init; }

    public required IPAddress Address { get; init; }

    public bool IsOperationalUp { get; init; } = true;

    public bool IsTemporary { get; init; }

    public bool IsDeprecated { get; init; }

    public SuffixOrigin SuffixOrigin { get; init; } = SuffixOrigin.Other;
}

public interface ILocalAddressSource
{
    IEnumerable<LocalAddressCandidate> GetCandidates();
}
