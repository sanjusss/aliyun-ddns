using System.Globalization;
using AliyunDdns.Domain;
using Microsoft.Extensions.Configuration;

namespace AliyunDdns.Configuration;

public sealed class LegacyOptionsReader
{
    private static readonly string[] LegacyNames =
    [
        "AKID", "AKSCT", "DOMAIN", "ROOT_DOMAIN", "REDO", "TTL", "TIMEZONE", "TYPE",
        "CNIPV4", "WEBHOOK", "CHECKLOCAL", "IPV4NETS", "IPV6NETS"
    ];

    public IReadOnlyList<string> Apply(
        DdnsOptions options,
        IConfiguration configuration,
        ILogger logger,
        Func<string, string?>? getEnvironmentVariable = null)
    {
        getEnvironmentVariable ??= Environment.GetEnvironmentVariable;
        var values = LegacyNames.Select(name => (Name: name, Value: getEnvironmentVariable(name)))
            .Where(item => !string.IsNullOrWhiteSpace(item.Value))
            .ToDictionary(item => item.Name, item => item.Value!, StringComparer.OrdinalIgnoreCase);
        if (values.Count == 0)
        {
            return [];
        }

        logger.LogWarning("检测到旧环境变量 {Variables}；请迁移到 JSON 或新版环境变量。", string.Join(',', values.Keys));
        var errors = new List<string>();
        if (values.TryGetValue("AKID", out var akid) && !HasValue(configuration, "Aliyun:AccessKeyId"))
        {
            options.Aliyun.AccessKeyId = akid;
        }

        if (values.TryGetValue("AKSCT", out var secret) && !HasValue(configuration, "Aliyun:AccessKeySecret") &&
            !HasValue(configuration, "Aliyun:AccessKeySecretFile"))
        {
            options.Aliyun.AccessKeySecret = secret;
        }

        if (values.TryGetValue("REDO", out var redo) && configuration["Updater:IntervalSeconds"] is null)
        {
            if (int.TryParse(redo, NumberStyles.Integer, CultureInfo.InvariantCulture, out var interval))
            {
                options.Updater.IntervalSeconds = interval;
            }
            else
            {
                errors.Add("旧变量 REDO 不是有效整数。");
            }
        }

        if (values.TryGetValue("TIMEZONE", out var timezone) && configuration["Updater:LogTimeZoneOffsetHours"] is null)
        {
            if (double.TryParse(timezone, NumberStyles.Float, CultureInfo.InvariantCulture, out var offset))
            {
                options.Updater.LogTimeZoneOffsetHours = offset;
            }
            else
            {
                errors.Add("旧变量 TIMEZONE 不是有效数字。");
            }
        }

        if (values.TryGetValue("CNIPV4", out var cnipv4) && configuration["Updater:ChinaIPv4Only"] is null)
        {
            if (bool.TryParse(cnipv4, out var chinaOnly))
            {
                options.Updater.ChinaIPv4Only = chinaOnly;
            }
            else
            {
                errors.Add("旧变量 CNIPV4 不是有效布尔值。");
            }
        }

        if (values.TryGetValue("WEBHOOK", out var webhook) && !HasValue(configuration, "Webhook:Url"))
        {
            options.Webhook.Url = webhook;
        }

        if (values.TryGetValue("DOMAIN", out var domains) && !HasValue(configuration, "Targets:0:Domain"))
        {
            ConvertTargets(options, values, domains, errors);
        }

        return errors;
    }

    private static void ConvertTargets(
        DdnsOptions options,
        IReadOnlyDictionary<string, string> values,
        string domains,
        ICollection<string> errors)
    {
        var types = ParseTypes(values.GetValueOrDefault("TYPE", "A,AAAA"), errors);
        var ttl = ParseTtl(values.GetValueOrDefault("TTL"), errors);
        var local = false;
        if (values.TryGetValue("CHECKLOCAL", out var checkLocal) && !bool.TryParse(checkLocal, out local))
        {
            errors.Add("旧变量 CHECKLOCAL 不是有效布尔值。");
        }

        var networks = new List<string>();
        networks.AddRange(Split(values.GetValueOrDefault("IPV4NETS")));
        networks.AddRange(Split(values.GetValueOrDefault("IPV6NETS")));
        var rootDomain = values.GetValueOrDefault("ROOT_DOMAIN");

        options.Targets = Split(domains).Select(item =>
        {
            var parts = item.Split(':', 2, StringSplitOptions.TrimEntries);
            return new DdnsTarget
            {
                Domain = parts.Length == 2 ? parts[1] : parts[0],
                Line = parts.Length == 2 ? parts[0] : "default",
                RootDomain = rootDomain,
                Types = types.ToList(),
                Ttl = ttl,
                Source = local ? IpAddressSource.Local : IpAddressSource.Public,
                Networks = networks.ToList()
            };
        }).ToList();
    }

    private static IReadOnlyList<DnsRecordType> ParseTypes(string? text, ICollection<string> errors)
    {
        var result = new List<DnsRecordType>();
        foreach (var item in Split(text))
        {
            if (Enum.TryParse<DnsRecordType>(item, true, out var type) && type is DnsRecordType.A or DnsRecordType.AAAA)
            {
                if (!result.Contains(type))
                {
                    result.Add(type);
                }
            }
            else
            {
                errors.Add($"旧变量 TYPE 包含不支持的记录类型 {item}。");
            }
        }

        return result;
    }

    private static long ParseTtl(string? text, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 600;
        }

        if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ttl))
        {
            return ttl;
        }

        errors.Add("旧变量 TTL 不是有效整数。");
        return 600;
    }

    private static bool HasValue(IConfiguration configuration, string key)
    {
        return !string.IsNullOrWhiteSpace(configuration[key]);
    }

    private static IEnumerable<string> Split(string? text)
    {
        return (text ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
