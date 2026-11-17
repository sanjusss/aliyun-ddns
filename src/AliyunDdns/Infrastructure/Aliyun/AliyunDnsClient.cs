using System.Net;
using AlibabaCloud.OpenApiClient.Models;
using AlibabaCloud.SDK.Alidns20150109;
using AlibabaCloud.SDK.Alidns20150109.Models;
using AliyunDdns.Application;
using AliyunDdns.Configuration;
using AliyunDdns.Domain;
using AliyunDdns.Infrastructure.Resilience;
using Microsoft.Extensions.Options;

namespace AliyunDdns.Infrastructure.Aliyun;

public sealed class AliyunDnsClient : IDnsClient
{
    private readonly Client _client;
    private readonly RequestRetryPolicy _retryPolicy;

    public AliyunDnsClient(IOptions<DdnsOptions> options, RequestRetryPolicy retryPolicy)
    {
        _retryPolicy = retryPolicy;
        var value = options.Value;
        var timeout = value.Updater.RequestTimeoutSeconds * 1000;
        _client = new Client(new Config
        {
            AccessKeyId = value.Aliyun.AccessKeyId,
            AccessKeySecret = SecretResolver.ResolveAccessKeySecret(value),
            Endpoint = value.Aliyun.Endpoint,
            Protocol = "HTTPS",
            ReadTimeout = timeout,
            ConnectTimeout = timeout,
            UserAgent = $"aliyun-ddns/{typeof(AliyunDnsClient).Assembly.GetName().Version?.ToString(3) ?? "0.3.0"}"
        });
    }

    public async Task<IReadOnlyList<string>> GetManagedDomainsAsync(CancellationToken cancellationToken)
    {
        var result = new List<string>();
        for (var page = 1; ; page++)
        {
            var response = await _retryPolicy.ExecuteAsync(async token =>
            {
                var result = await _client.DescribeDomainsAsync(new DescribeDomainsRequest
                {
                    PageNumber = page,
                    PageSize = 100
                }).WaitAsync(token);
                EnsureSuccess(result.StatusCode);
                return result;
            }, cancellationToken);
            var body = response.Body ?? throw new InvalidDataException("阿里云返回了空域名列表。");
            var domains = body.Domains?.Domain ?? [];
            result.AddRange(domains.Where(domain => !string.IsNullOrWhiteSpace(domain.DomainName))
                .Select(domain => domain.DomainName));
            if (domains.Count == 0 || result.Count >= body.TotalCount)
            {
                break;
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<DnsRecord>> GetRecordsAsync(
        ResolvedTarget target,
        CancellationToken cancellationToken)
    {
        var result = new List<DnsRecord>();
        for (var page = 1; ; page++)
        {
            var response = await _retryPolicy.ExecuteAsync(async token =>
            {
                var result = await _client.DescribeSubDomainRecordsAsync(new DescribeSubDomainRecordsRequest
                {
                    DomainName = target.ZoneName,
                    SubDomain = target.Domain,
                    Line = target.Target.Line,
                    PageNumber = page,
                    PageSize = 100
                }).WaitAsync(token);
                EnsureSuccess(result.StatusCode);
                return result;
            }, cancellationToken);
            var body = response.Body ?? throw new InvalidDataException("阿里云返回了空解析记录列表。");
            var records = body.DomainRecords?.Record ?? [];
            result.AddRange(records.Select(MapRecord));
            if (records.Count == 0 || result.Count >= body.TotalCount)
            {
                break;
            }
        }

        return result;
    }

    public async Task<DnsRecord> AddAsync(
        ResolvedTarget target,
        DnsRecordType type,
        IPAddress address,
        CancellationToken cancellationToken)
    {
        var response = await _client.AddDomainRecordAsync(new AddDomainRecordRequest
        {
            DomainName = target.ZoneName,
            RR = target.HostRecord,
            Type = type.ToString(),
            Value = address.ToString(),
            TTL = target.Target.Ttl,
            Line = target.Target.Line
        }).WaitAsync(cancellationToken);
        EnsureSuccess(response.StatusCode);
        var recordId = response.Body?.RecordId;
        if (string.IsNullOrWhiteSpace(recordId))
        {
            throw new InvalidDataException("阿里云新增记录后未返回 RecordId。");
        }

        return new DnsRecord
        {
            RecordId = recordId,
            DomainName = target.ZoneName,
            HostRecord = target.HostRecord,
            Type = type,
            Value = address.ToString(),
            Ttl = target.Target.Ttl,
            Line = target.Target.Line
        };
    }

    public async Task UpdateAsync(
        ResolvedTarget target,
        DnsRecord record,
        IPAddress address,
        CancellationToken cancellationToken)
    {
        await _retryPolicy.ExecuteAsync(async token =>
        {
            var response = await _client.UpdateDomainRecordAsync(new UpdateDomainRecordRequest
            {
                RecordId = record.RecordId,
                RR = record.HostRecord,
                Type = record.Type.ToString(),
                Value = address.ToString(),
                TTL = target.Target.Ttl,
                Line = record.Line
            }).WaitAsync(token);
            EnsureSuccess(response.StatusCode);
            return response;
        }, cancellationToken);
    }

    public async Task DeleteAsync(DnsRecord record, CancellationToken cancellationToken)
    {
        var response = await _client.DeleteDomainRecordAsync(new DeleteDomainRecordRequest
        {
            RecordId = record.RecordId
        }).WaitAsync(cancellationToken);
        EnsureSuccess(response.StatusCode);
    }

    private static DnsRecord MapRecord(DescribeSubDomainRecordsResponseBody.DescribeSubDomainRecordsResponseBodyDomainRecords.DescribeSubDomainRecordsResponseBodyDomainRecordsRecord record)
    {
        return new DnsRecord
        {
            RecordId = record.RecordId,
            DomainName = record.DomainName,
            HostRecord = record.RR,
            Type = record.Type?.ToUpperInvariant() switch
            {
                "A" => DnsRecordType.A,
                "AAAA" => DnsRecordType.AAAA,
                "CNAME" => DnsRecordType.CNAME,
                _ => DnsRecordType.Other
            },
            Value = record.Value,
            Ttl = record.TTL ?? 600,
            Line = record.Line ?? "default"
        };
    }

    private static void EnsureSuccess(int? statusCode)
    {
        if (statusCode is < 200 or >= 300 or null)
        {
            throw new HttpRequestException($"阿里云 API 返回 HTTP {statusCode?.ToString() ?? "未知"}。", null,
                statusCode.HasValue ? (HttpStatusCode)statusCode.Value : null);
        }
    }
}
