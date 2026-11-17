namespace AliyunDdns.Infrastructure.Ip;

public sealed class NetworkInterfaceLocalAddressSource : ILocalAddressSource
{
    private readonly INetworkInterfaceInventory _inventory;
    private readonly ILocalAddressMetadataSource _metadataSource;

    public NetworkInterfaceLocalAddressSource(
        INetworkInterfaceInventory inventory,
        ILocalAddressMetadataSource metadataSource)
    {
        _inventory = inventory;
        _metadataSource = metadataSource;
    }

    public IEnumerable<LocalAddressCandidate> GetCandidates()
    {
        var metadata = _metadataSource.GetMetadata();
        foreach (var candidate in _inventory.GetCandidates())
        {
            if (!metadata.TryGetValue(new LocalAddressKey(candidate.InterfaceName, candidate.Address), out var value))
            {
                yield return candidate;
                continue;
            }

            yield return candidate with
            {
                IsTemporary = value.IsTemporary,
                IsDeprecated = value.IsDeprecated,
                SuffixOrigin = value.SuffixOrigin ?? candidate.SuffixOrigin
            };
        }
    }
}
