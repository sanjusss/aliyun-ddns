using System.Net;
using AliyunDdns.Application;
using AliyunDdns.Domain;

namespace AliyunDdns.Tests;

internal sealed class FakeDnsClient : IDnsClient
{
    public IReadOnlyList<string> ManagedDomains { get; set; } = ["example.com"];

    public Dictionary<string, IReadOnlyList<DnsRecord>> Records { get; } = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> FailingDomains { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<DnsRecord> Added { get; } = [];

    public List<DnsRecord> Updated { get; } = [];

    public List<DnsRecord> Deleted { get; } = [];

    public int GetManagedDomainsCallCount { get; private set; }

    public int GetRecordsCallCount { get; private set; }

    public Task<IReadOnlyList<string>> GetManagedDomainsAsync(CancellationToken cancellationToken)
    {
        GetManagedDomainsCallCount++;
        return Task.FromResult(ManagedDomains);
    }

    public Task<IReadOnlyList<DnsRecord>> GetRecordsAsync(ResolvedTarget target, CancellationToken cancellationToken)
    {
        GetRecordsCallCount++;
        if (FailingDomains.Contains(target.Domain))
        {
            throw new InvalidOperationException("模拟 DNS 查询失败。");
        }

        return Task.FromResult(Records.GetValueOrDefault(target.Domain, []));
    }

    public Task<DnsRecord> AddAsync(ResolvedTarget target, DnsRecordType type, IPAddress address, CancellationToken cancellationToken)
    {
        var record = new DnsRecord
        {
            RecordId = (Added.Count + Updated.Count + Deleted.Count + 100).ToString(),
            DomainName = target.ZoneName,
            HostRecord = target.HostRecord,
            Type = type,
            Value = address.ToString(),
            Ttl = target.Target.Ttl,
            Line = target.Target.Line
        };
        Added.Add(record);
        return Task.FromResult(record);
    }

    public Task UpdateAsync(ResolvedTarget target, DnsRecord record, IPAddress address, CancellationToken cancellationToken)
    {
        Updated.Add(record with { Value = address.ToString(), Ttl = target.Target.Ttl });
        return Task.CompletedTask;
    }

    public Task DeleteAsync(DnsRecord record, CancellationToken cancellationToken)
    {
        Deleted.Add(record);
        return Task.CompletedTask;
    }
}
