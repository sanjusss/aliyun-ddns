using System.Net;
using AliyunDdns.Configuration;
using AliyunDdns.Domain;

namespace AliyunDdns.Application;

public interface IIpAddressProvider
{
    IpAddressSource Source { get; }

    Task<IPAddress?> GetAddressAsync(
        DdnsTarget target,
        DnsRecordType type,
        CancellationToken cancellationToken);
}
