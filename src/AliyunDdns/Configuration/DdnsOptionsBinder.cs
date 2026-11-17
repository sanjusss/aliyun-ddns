using System.Globalization;
using AliyunDdns.Domain;
using Microsoft.Extensions.Configuration;

namespace AliyunDdns.Configuration;

public static class DdnsOptionsBinder
{
    public static DdnsOptions Bind(IConfiguration configuration)
    {
        var options = new DdnsOptions();
        configuration.GetSection("Updater").Bind(options.Updater);
        configuration.GetSection("Aliyun").Bind(options.Aliyun);
        configuration.GetSection("Webhook").Bind(options.Webhook);

        foreach (var section in configuration.GetSection("Targets").GetChildren())
        {
            ValidateScalarBindings(section);
            var target = new DdnsTarget();
            try
            {
                section.Bind(target, options => options.ErrorOnUnknownConfiguration = true);
            }
            catch (Exception exception) when (exception is InvalidOperationException or FormatException)
            {
                throw new InvalidOperationException($"{section.Path}: 目标配置转换失败：{exception.Message}", exception);
            }

            if (!section.GetSection("Types").GetChildren().Any() && section.GetSection("Types").Value is null)
            {
                target.Types = [DnsRecordType.A, DnsRecordType.AAAA];
            }

            options.Targets.Add(target);
        }

        return options;
    }

    private static void ValidateScalarBindings(IConfigurationSection section)
    {
        ValidateEnum<IpAddressSource>(section, "Source");
        ValidateEnum<LocalAddressPolicy>(section, "AddressPolicy");
        if (section["Ttl"] is { } ttl &&
            !long.TryParse(ttl, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            throw new InvalidOperationException($"{section.Path}:Ttl 的值 {ttl} 不是有效整数。");
        }

        foreach (var type in section.GetSection("Types").GetChildren())
        {
            if (!Enum.TryParse<DnsRecordType>(type.Value, true, out var recordType) || !Enum.IsDefined(recordType))
            {
                throw new InvalidOperationException($"{type.Path}: 无法转换记录类型 {type.Value}。");
            }
        }
    }

    private static void ValidateEnum<TEnum>(IConfigurationSection section, string key)
        where TEnum : struct, Enum
    {
        if (section[key] is { } value && (!Enum.TryParse<TEnum>(value, true, out var parsed) || !Enum.IsDefined(parsed)))
        {
            throw new InvalidOperationException($"{section.Path}:{key} 的值 {value} 无法转换为 {typeof(TEnum).Name}。");
        }
    }
}
