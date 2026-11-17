using System.Net.NetworkInformation;

namespace AliyunDdns.Infrastructure.Ip;

public sealed class NetworkInterfaceInventory : INetworkInterfaceInventory
{
    public IEnumerable<LocalAddressCandidate> GetCandidates()
    {
        foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (adapter.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                continue;
            }

            UnicastIPAddressInformationCollection addresses;
            try
            {
                addresses = adapter.GetIPProperties().UnicastAddresses;
            }
            catch (NetworkInformationException)
            {
                continue;
            }

            foreach (var address in addresses)
            {
                SuffixOrigin suffixOrigin = SuffixOrigin.Other;
                var temporary = false;
                var deprecated = false;
                if (OperatingSystem.IsWindows())
                {
                    try
                    {
                        suffixOrigin = address.SuffixOrigin;
                        temporary = suffixOrigin == SuffixOrigin.Random;
                        deprecated = address.DuplicateAddressDetectionState == DuplicateAddressDetectionState.Deprecated;
                    }
                    catch (PlatformNotSupportedException)
                    {
                        // Windows 的个别网卡也可能不提供地址来源信息。
                    }
                }

                yield return new LocalAddressCandidate
                {
                    InterfaceName = adapter.Name,
                    Address = address.Address,
                    IsOperationalUp = adapter.OperationalStatus == OperationalStatus.Up,
                    IsTemporary = temporary,
                    IsDeprecated = deprecated,
                    SuffixOrigin = suffixOrigin
                };
            }
        }
    }
}
