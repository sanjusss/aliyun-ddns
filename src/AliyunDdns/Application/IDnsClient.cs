using System.Net;
using AliyunDdns.Domain;

namespace AliyunDdns.Application;

public interface IDnsClient
{
    Task<IReadOnlyList<string>> GetManagedDomainsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<DnsRecord>> GetRecordsAsync(
        ResolvedTarget target,
        CancellationToken cancellationToken);

    Task<DnsRecord> AddAsync(
        ResolvedTarget target,
        DnsRecordType type,
        IPAddress address,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        ResolvedTarget target,
        DnsRecord record,
        IPAddress address,
        CancellationToken cancellationToken);

    Task DeleteAsync(DnsRecord record, CancellationToken cancellationToken);
}
