using System.Net;
using System.Net.Sockets;

namespace AliyunDdns.Infrastructure.Ip;

public static class IpAddressValidator
{
    private static readonly IPNetwork[] NonGlobalIPv6Ranges =
    [
        IPNetwork.Parse("64:ff9b:1::/48"),
        IPNetwork.Parse("100::/64"),
        IPNetwork.Parse("100:0:0:1::/64"),
        IPNetwork.Parse("2001:2::/48"),
        IPNetwork.Parse("2001:10::/28"),
        IPNetwork.Parse("2001:20::/28"), // ORCHIDv2 专用标识，不作为普通公网 DDNS 地址。
        IPNetwork.Parse("2001:db8::/32"),
        IPNetwork.Parse("3fff::/20"),
        IPNetwork.Parse("5f00::/16"),
        IPNetwork.Parse("fc00::/7")
    ];
    public static bool HasExpectedFamily(IPAddress address, Domain.DnsRecordType type)
    {
        return type switch
        {
            Domain.DnsRecordType.A => address.AddressFamily == AddressFamily.InterNetwork,
            Domain.DnsRecordType.AAAA => address.AddressFamily == AddressFamily.InterNetworkV6,
            _ => false
        };
    }

    public static bool TryParsePublic(string text, Domain.DnsRecordType type, out IPAddress? address)
    {
        address = null;
        if (!IPAddress.TryParse(text, out var parsed) || !HasExpectedFamily(parsed, type) || !IsPublic(parsed))
        {
            return false;
        }

        address = parsed;
        return true;
    }

    public static bool IsPublic(IPAddress address)
    {
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) ||
            address.Equals(IPAddress.Broadcast) || address.IsIPv6LinkLocal || address.IsIPv6Multicast ||
            address.IsIPv6SiteLocal || address.IsIPv4MappedToIPv6)
        {
            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = address.GetAddressBytes();
            if (bytes.Take(12).All(value => value == 0))
            {
                return false;
            }

            return !NonGlobalIPv6Ranges.Any(network => network.Contains(address));
        }

        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var ipv4 = address.GetAddressBytes();
        return !(ipv4[0] == 0 || ipv4[0] == 10 || ipv4[0] == 127 || ipv4[0] >= 224 ||
                 (ipv4[0] == 100 && ipv4[1] >= 64 && ipv4[1] <= 127) ||
                 (ipv4[0] == 169 && ipv4[1] == 254) ||
                 (ipv4[0] == 172 && ipv4[1] >= 16 && ipv4[1] <= 31) ||
                 (ipv4[0] == 192 && ipv4[1] == 168) ||
                 (ipv4[0] == 192 && ipv4[1] == 0 && ipv4[2] == 0) ||
                 (ipv4[0] == 192 && ipv4[1] == 0 && ipv4[2] == 2) ||
                 (ipv4[0] == 198 && (ipv4[1] == 18 || ipv4[1] == 19)) ||
                 (ipv4[0] == 198 && ipv4[1] == 51 && ipv4[2] == 100) ||
                 (ipv4[0] == 203 && ipv4[1] == 0 && ipv4[2] == 113));
    }

    public static bool IsUsableLocal(IPAddress address)
    {
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) ||
            address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.Broadcast) ||
            address.IsIPv6LinkLocal || address.IsIPv6Multicast)
        {
            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return true;
        }

        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] != 0 && bytes[0] < 224 && !(bytes[0] == 169 && bytes[1] == 254);
    }
}
