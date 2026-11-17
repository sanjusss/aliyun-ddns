using System.Net;
using AliyunDdns.Configuration;
using AliyunDdns.Domain;
using AliyunDdns.Infrastructure.Ip;

namespace AliyunDdns.Application;

public sealed class DdnsUpdatePlanner
{
    public UpdatePlan Build(
        ResolvedTarget target,
        IReadOnlyDictionary<DnsRecordType, IPAddress?> desiredAddresses,
        IReadOnlyList<DnsRecord> currentRecords,
        ConflictPolicy conflictPolicy)
    {
        var operations = new List<DnsOperation>();
        var validTypes = new List<DnsRecordType>();

        foreach (var type in target.Target.Types.Distinct())
        {
            desiredAddresses.TryGetValue(type, out var address);
            if (address is null || !IpAddressValidator.HasExpectedFamily(address, type))
            {
                operations.Add(Skip(type, "未获取到有效 IP，禁止执行写操作。"));
                continue;
            }

            if (currentRecords.Any(record => record.Type == DnsRecordType.CNAME) && conflictPolicy == ConflictPolicy.Skip)
            {
                operations.Add(Skip(type, "存在 CNAME 冲突，ConflictPolicy=Skip。"));
                continue;
            }

            validTypes.Add(type);
        }

        if (validTypes.Count > 0 && conflictPolicy == ConflictPolicy.Replace)
        {
            foreach (var cname in currentRecords.Where(record => record.Type == DnsRecordType.CNAME)
                         .OrderBy(record => record.RecordId, StringComparer.Ordinal))
            {
                operations.Add(new DnsOperation
                {
                    Kind = DnsOperationKind.Delete,
                    Type = DnsRecordType.CNAME,
                    ExistingRecord = cname,
                    Reason = "ConflictPolicy=Replace，删除明确授权的 CNAME 冲突。"
                });
            }
        }

        foreach (var type in validTypes)
        {
            var address = desiredAddresses[type]!;
            var records = currentRecords.Where(record => record.Type == type)
                .OrderBy(record => record.RecordId, StringComparer.Ordinal)
                .ToArray();

            if (records.Length == 0)
            {
                operations.Add(new DnsOperation
                {
                    Kind = DnsOperationKind.Add,
                    Type = type,
                    DesiredAddress = address.ToString(),
                    Reason = "没有对应记录，新增解析记录。"
                });
                continue;
            }

            if (records.Length > 1 && conflictPolicy == ConflictPolicy.Skip)
            {
                operations.Add(Skip(type, $"发现 {records.Length} 条同类型记录，ConflictPolicy=Skip。"));
                continue;
            }

            var primary = records[0];
            if (!string.Equals(primary.Value, address.ToString(), StringComparison.Ordinal) ||
                primary.Ttl != target.Target.Ttl)
            {
                operations.Add(new DnsOperation
                {
                    Kind = DnsOperationKind.Update,
                    Type = type,
                    ExistingRecord = primary,
                    DesiredAddress = address.ToString(),
                    Reason = "现有记录的 IP 或 TTL 与期望值不同，按 RecordId 更新。"
                });
            }
            else
            {
                operations.Add(new DnsOperation
                {
                    Kind = DnsOperationKind.None,
                    Type = type,
                    ExistingRecord = primary,
                    DesiredAddress = address.ToString(),
                    Reason = "记录的 IP 和 TTL 均符合期望，无需更新。"
                });
            }

            if (records.Length > 1)
            {
                foreach (var duplicate in records.Skip(1))
                {
                    operations.Add(new DnsOperation
                    {
                        Kind = DnsOperationKind.Delete,
                        Type = type,
                        ExistingRecord = duplicate,
                        Reason = "ConflictPolicy=Replace，删除明确授权的多余记录。"
                    });
                }
            }
        }

        return new UpdatePlan { Target = target, Operations = operations };
    }

    private static DnsOperation Skip(DnsRecordType type, string reason)
    {
        return new DnsOperation { Kind = DnsOperationKind.Skip, Type = type, Reason = reason };
    }
}
