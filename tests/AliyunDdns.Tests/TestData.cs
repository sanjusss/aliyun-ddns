using System.Net;
using AliyunDdns.Application;
using AliyunDdns.Configuration;
using AliyunDdns.Domain;
using AliyunDdns.Infrastructure.Ip;

namespace AliyunDdns.Tests;

internal static class TestData
{
    public static DdnsTarget Target(string domain = "home.example.com", IpAddressSource source = IpAddressSource.Public)
    {
        return new DdnsTarget
        {
            Domain = domain,
            RootDomain = "example.com",
            Types = [DnsRecordType.A],
            Source = source
        };
    }

    public static ResolvedTarget Resolved(string domain = "home.example.com", IpAddressSource source = IpAddressSource.Public)
    {
        return new ResolvedTarget
        {
            Target = Target(domain, source),
            Domain = domain.Trim().TrimEnd('.').ToLowerInvariant(),
            ZoneName = "example.com",
            HostRecord = "home"
        };
    }

    public static DnsRecord Record(
        DnsRecordType type = DnsRecordType.A,
        string value = "1.1.1.1",
        string id = "1",
        string domain = "example.com",
        long ttl = 600)
    {
        return new DnsRecord
        {
            RecordId = id,
            DomainName = domain,
            HostRecord = "home",
            Type = type,
            Value = value,
            Ttl = ttl
        };
    }

    public static DdnsOptions Options(params DdnsTarget[] targets)
    {
        return new DdnsOptions
        {
            Aliyun = new AliyunOptions { AccessKeyId = "test-id", AccessKeySecret = "test-secret" },
            Targets = targets.ToList()
        };
    }
}

internal sealed class FakeIpProvider(IpAddressSource source, Func<DdnsTarget, DnsRecordType, IPAddress?> getAddress) : IIpAddressProvider
{
    public IpAddressSource Source => source;

    public Task<IPAddress?> GetAddressAsync(DdnsTarget target, DnsRecordType type, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(getAddress(target, type));
    }
}

internal sealed class FakeNotificationSink : INotificationSink
{
    public int CallCount { get; private set; }

    public UpdateResult? Result { get; private set; }

    public Task NotifyAsync(UpdateResult result, CancellationToken cancellationToken)
    {
        CallCount++;
        Result = result;
        return Task.CompletedTask;
    }
}

internal sealed class FakeLocalAddressSource(IEnumerable<LocalAddressCandidate> candidates) : ILocalAddressSource
{
    public IEnumerable<LocalAddressCandidate> GetCandidates() => candidates;
}
