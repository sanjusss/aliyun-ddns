using System.Net;
using System.Net.NetworkInformation;
using AliyunDdns.Application;
using AliyunDdns.Configuration;
using AliyunDdns.Domain;

namespace AliyunDdns.Infrastructure.Ip;

public sealed class LocalIpAddressProvider : IIpAddressProvider
{
    private readonly ILocalAddressSource _source;

    public LocalIpAddressProvider(ILocalAddressSource source)
    {
        _source = source;
    }

    public IpAddressSource Source => IpAddressSource.Local;

    public Task<IPAddress?> GetAddressAsync(
        DdnsTarget target,
        DnsRecordType type,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var networks = target.Networks.Select(network => IPNetwork.TryParse(network, out var parsed) ? parsed : (IPNetwork?)null)
            .Where(network => network is not null)
            .Select(network => network!.Value)
            .ToArray();

        var candidates = _source.GetCandidates().Where(candidate =>
                candidate.IsOperationalUp &&
                !candidate.IsDeprecated &&
                IpAddressValidator.HasExpectedFamily(candidate.Address, type) &&
                IpAddressValidator.IsUsableLocal(candidate.Address) &&
                (target.AllowAnyInterface || !LocalInterfaceFilter.IsCommonContainerBridge(candidate.InterfaceName)) &&
                (string.IsNullOrWhiteSpace(target.Interface) ||
                 candidate.InterfaceName.Equals(target.Interface, StringComparison.OrdinalIgnoreCase)) &&
                (networks.Length == 0 || networks.Any(network => network.Contains(candidate.Address))))
            .ToArray();

        var selected = target.AddressPolicy switch
        {
            LocalAddressPolicy.Eui64 => candidates.OrderBy(candidate => !IsEui64(candidate.Address))
                .ThenBy(candidate => candidate.IsTemporary)
                .ThenBy(candidate => StabilityRank(candidate.SuffixOrigin))
                .ThenBy(candidate => candidate.InterfaceName, StringComparer.Ordinal)
                .ThenBy(AddressSortKey, StringComparer.Ordinal)
                .FirstOrDefault(),
            LocalAddressPolicy.First => candidates.OrderBy(candidate => candidate.InterfaceName, StringComparer.Ordinal)
                .ThenBy(AddressSortKey, StringComparer.Ordinal)
                .FirstOrDefault(),
            _ => candidates.OrderBy(candidate => candidate.IsTemporary)
                .ThenBy(candidate => StabilityRank(candidate.SuffixOrigin))
                .ThenBy(candidate => candidate.InterfaceName, StringComparer.Ordinal)
                .ThenBy(AddressSortKey, StringComparer.Ordinal)
                .FirstOrDefault()
        };

        return Task.FromResult(selected?.Address);
    }

    private static int StabilityRank(SuffixOrigin origin)
    {
        return origin switch
        {
            SuffixOrigin.Manual => 0,
            SuffixOrigin.LinkLayerAddress => 1,
            SuffixOrigin.OriginDhcp => 2,
            SuffixOrigin.WellKnown => 3,
            SuffixOrigin.Random => 5,
            _ => 4
        };
    }

    private static bool IsEui64(IPAddress address)
    {
        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        return bytes[11] == 0xff && bytes[12] == 0xfe;
    }

    private static string AddressSortKey(LocalAddressCandidate candidate)
    {
        return Convert.ToHexString(candidate.Address.GetAddressBytes());
    }
}
