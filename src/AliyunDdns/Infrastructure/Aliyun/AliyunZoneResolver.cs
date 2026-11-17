using AliyunDdns.Application;
using AliyunDdns.Configuration;
using AliyunDdns.Domain;

namespace AliyunDdns.Infrastructure.Aliyun;

public sealed class AliyunZoneResolver
{
    private readonly IDnsClient _dnsClient;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private IReadOnlyList<string>? _managedDomains;

    public AliyunZoneResolver(IDnsClient dnsClient)
    {
        _dnsClient = dnsClient;
    }

    public async Task<ResolvedTarget> ResolveAsync(DdnsTarget target, CancellationToken cancellationToken)
    {
        var domain = Normalize(target.Domain);
        var zone = string.IsNullOrWhiteSpace(target.RootDomain)
            ? await FindLongestManagedZoneAsync(domain, cancellationToken)
            : Normalize(target.RootDomain);

        var hostRecord = domain.Equals(zone, StringComparison.OrdinalIgnoreCase)
            ? "@"
            : domain.EndsWith($".{zone}", StringComparison.OrdinalIgnoreCase)
                ? domain[..^(zone.Length + 1)]
                : throw new InvalidOperationException($"域名 {target.Domain} 不属于主域名 {zone}。");

        return new ResolvedTarget { Target = target, Domain = domain, ZoneName = zone, HostRecord = hostRecord };
    }

    private async Task<string> FindLongestManagedZoneAsync(string domain, CancellationToken cancellationToken)
    {
        var domains = await GetManagedDomainsAsync(cancellationToken);
        var zone = domains.Select(Normalize)
            .Where(candidate => domain.Equals(candidate, StringComparison.OrdinalIgnoreCase) ||
                                domain.EndsWith($".{candidate}", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(candidate => candidate.Length)
            .ThenBy(candidate => candidate, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        return zone ?? throw new InvalidOperationException($"阿里云账号中找不到 {domain} 对应的托管主域名。");
    }

    private async Task<IReadOnlyList<string>> GetManagedDomainsAsync(CancellationToken cancellationToken)
    {
        if (_managedDomains is not null)
        {
            return _managedDomains;
        }

        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            return _managedDomains ??= await _dnsClient.GetManagedDomainsAsync(cancellationToken);
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private static string Normalize(string domain)
    {
        return domain.Trim().TrimEnd('.').ToLowerInvariant();
    }
}
