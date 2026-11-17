using System.Net;
using System.Net.NetworkInformation;

namespace AliyunDdns.Infrastructure.Ip;

public sealed record LocalAddressKey(string InterfaceName, IPAddress Address);

public sealed record LocalAddressMetadata
{
    public bool IsTemporary { get; init; }

    public bool IsDeprecated { get; init; }

    public SuffixOrigin? SuffixOrigin { get; init; }
}

public interface ILocalAddressMetadataSource
{
    IReadOnlyDictionary<LocalAddressKey, LocalAddressMetadata> GetMetadata();
}

public interface INetworkInterfaceInventory
{
    IEnumerable<LocalAddressCandidate> GetCandidates();
}
