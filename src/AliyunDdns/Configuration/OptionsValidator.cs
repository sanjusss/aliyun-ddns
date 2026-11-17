using System.Net;

namespace AliyunDdns.Configuration;

public static class OptionsValidator
{
    public static IReadOnlyList<string> Validate(DdnsOptions options)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(options.Aliyun.AccessKeyId) ||
            options.Aliyun.AccessKeyId == "access key id")
        {
            errors.Add("Aliyun:AccessKeyId 不能为空。");
        }

        if (string.IsNullOrWhiteSpace(options.Aliyun.AccessKeySecretFile) &&
            (string.IsNullOrWhiteSpace(options.Aliyun.AccessKeySecret) ||
             options.Aliyun.AccessKeySecret == "access key secret"))
        {
            errors.Add("必须设置 Aliyun:AccessKeySecret 或 Aliyun:AccessKeySecretFile。");
        }

        if (!string.IsNullOrWhiteSpace(options.Aliyun.AccessKeySecretFile) &&
            !File.Exists(options.Aliyun.AccessKeySecretFile))
        {
            errors.Add($"Secret 文件不存在：{options.Aliyun.AccessKeySecretFile}");
        }

        if (string.IsNullOrWhiteSpace(options.Aliyun.Endpoint))
        {
            errors.Add("Aliyun:Endpoint 不能为空。");
        }

        if (options.Updater.IntervalSeconds < 5)
        {
            errors.Add("Updater:IntervalSeconds 必须大于或等于 5。");
        }

        if (options.Updater.RequestTimeoutSeconds is < 1 or > 300)
        {
            errors.Add("Updater:RequestTimeoutSeconds 必须在 1 到 300 之间。");
        }

        if (options.Updater.RetryMaxAttempts is < 1 or > 5)
        {
            errors.Add("Updater:RetryMaxAttempts 必须在 1 到 5 之间。");
        }

        if (options.Updater.RetryBaseDelayMilliseconds is < 0 or > 10000)
        {
            errors.Add("Updater:RetryBaseDelayMilliseconds 必须在 0 到 10000 之间。");
        }

        if (!double.IsFinite(options.Updater.LogTimeZoneOffsetHours) ||
            options.Updater.LogTimeZoneOffsetHours is < -14 or > 14)
        {
            errors.Add("Updater:LogTimeZoneOffsetHours 必须在 -14 到 14 之间。");
        }

        if (double.IsFinite(options.Updater.LogTimeZoneOffsetHours) &&
            Math.Abs(options.Updater.LogTimeZoneOffsetHours * 60 -
                     Math.Round(options.Updater.LogTimeZoneOffsetHours * 60)) > 1e-9)
        {
            errors.Add("Updater:LogTimeZoneOffsetHours 必须表示整分钟偏移，例如 8.5 表示 8 小时 30 分。");
        }

        if (!Enum.IsDefined(options.Updater.ConflictPolicy))
        {
            errors.Add("Updater:ConflictPolicy 仅支持 Skip 或 Replace。");
        }

        if (!string.IsNullOrWhiteSpace(options.Webhook.Url) &&
            (!Uri.TryCreate(options.Webhook.Url, UriKind.Absolute, out var webhookUri) ||
             (webhookUri.Scheme != Uri.UriSchemeHttp && webhookUri.Scheme != Uri.UriSchemeHttps)))
        {
            errors.Add("Webhook:Url 必须是有效的 HTTP 或 HTTPS 地址。");
        }

        if (options.Webhook.TimeoutSeconds is < 1 or > 300)
        {
            errors.Add("Webhook:TimeoutSeconds 必须在 1 到 300 之间。");
        }

        if (options.Targets.Count == 0)
        {
            errors.Add("至少需要配置一个解析目标。");
        }

        for (var index = 0; index < options.Targets.Count; index++)
        {
            ValidateTarget(options.Targets[index], index, errors);
        }

        return errors;
    }

    private static void ValidateTarget(DdnsTarget target, int index, ICollection<string> errors)
    {
        var prefix = $"Targets:{index}";
        if (!IsValidTargetDomain(target.Domain))
        {
            errors.Add($"{prefix}:Domain 不是有效域名。");
        }

        if (!string.IsNullOrWhiteSpace(target.RootDomain))
        {
            if (!IsValidDomain(target.RootDomain))
            {
                errors.Add($"{prefix}:RootDomain 不是有效域名。");
            }
            else if (IsValidTargetDomain(target.Domain) &&
                     !target.Domain.TrimEnd('.').Equals(target.RootDomain.TrimEnd('.'), StringComparison.OrdinalIgnoreCase) &&
                     !target.Domain.TrimEnd('.').EndsWith($".{target.RootDomain.TrimEnd('.')}", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"{prefix}:Domain 必须属于 RootDomain。");
            }
        }

        if (target.Types.Count == 0 || target.Types.Any(type => type is not (Domain.DnsRecordType.A or Domain.DnsRecordType.AAAA)))
        {
            errors.Add($"{prefix}:Types 只能包含 A 或 AAAA，且不能为空。");
        }

        if (string.IsNullOrWhiteSpace(target.Line))
        {
            errors.Add($"{prefix}:Line 不能为空。");
        }

        if (target.Ttl is < 1 or > 86400)
        {
            errors.Add($"{prefix}:Ttl 必须在 1 到 86400 之间。");
        }

        if (!Enum.IsDefined(target.Source))
        {
            errors.Add($"{prefix}:Source 仅支持 Public 或 Local。");
        }

        if (!Enum.IsDefined(target.AddressPolicy))
        {
            errors.Add($"{prefix}:AddressPolicy 仅支持 Stable、Eui64 或 First。");
        }

        foreach (var network in target.Networks)
        {
            if (!IPNetwork.TryParse(network, out _))
            {
                errors.Add($"{prefix}:Networks 包含无效网段 {network}。");
            }
        }
    }

    private static bool IsValidTargetDomain(string? domain)
    {
        return domain is not null &&
               (domain.StartsWith("*.", StringComparison.Ordinal)
                   ? IsValidDomain(domain[2..])
                   : IsValidDomain(domain));
    }

    private static bool IsValidDomain(string? domain)
    {
        return !string.IsNullOrWhiteSpace(domain) &&
               !domain.Any(char.IsWhiteSpace) &&
               Uri.CheckHostName(domain.TrimEnd('.')) == UriHostNameType.Dns;
    }
}
