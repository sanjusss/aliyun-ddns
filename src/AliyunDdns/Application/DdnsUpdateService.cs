using System.Net;
using AliyunDdns.Configuration;
using AliyunDdns.Domain;
using AliyunDdns.Infrastructure.Aliyun;
using AliyunDdns.Infrastructure.Ip;
using Microsoft.Extensions.Options;

namespace AliyunDdns.Application;

public sealed class DdnsUpdateService
{
    private readonly IReadOnlyDictionary<IpAddressSource, IIpAddressProvider> _ipProviders;
    private readonly IDnsClient _dnsClient;
    private readonly AliyunZoneResolver _zoneResolver;
    private readonly DdnsUpdatePlanner _planner;
    private readonly INotificationSink _notificationSink;
    private readonly DdnsOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DdnsUpdateService> _logger;

    public DdnsUpdateService(
        IEnumerable<IIpAddressProvider> ipProviders,
        IDnsClient dnsClient,
        AliyunZoneResolver zoneResolver,
        DdnsUpdatePlanner planner,
        INotificationSink notificationSink,
        IOptions<DdnsOptions> options,
        TimeProvider timeProvider,
        ILogger<DdnsUpdateService> logger)
    {
        _ipProviders = ipProviders.ToDictionary(provider => provider.Source);
        _dnsClient = dnsClient;
        _zoneResolver = zoneResolver;
        _planner = planner;
        _notificationSink = notificationSink;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<UpdateResult> RunOnceAsync(CancellationToken cancellationToken)
    {
        var result = new UpdateResult
        {
            Timestamp = _timeProvider.GetUtcNow().ToOffset(
                TimeSpan.FromHours(_options.Updater.LogTimeZoneOffsetHours))
        };

        foreach (var target in _options.Targets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await ProcessTargetAsync(target, result, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                var error = LogSanitizer.Sanitize(exception.Message, _options);
                var domain = target.Domain.Trim().TrimEnd('.').ToLowerInvariant();
                result.Failures.Add(new TargetFailure { Domain = domain, Error = error });
                _logger.LogError("目标 {Domain} 更新失败：{Error}", domain, error);
            }
        }

        if (result.SuccessfulChanges.Count > 0)
        {
            try
            {
                await _notificationSink.NotifyAsync(result, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                result.NotificationFailed = true;
                _logger.LogError("发送变更通知失败：{Error}",
                    LogSanitizer.Sanitize(exception.Message, _options));
            }
        }

        return result;
    }

    internal async Task ExecutePlanAsync(UpdatePlan plan, UpdateResult result, CancellationToken cancellationToken)
    {
        ValidatePlanAuthorization(plan);
        ValidatePlanAddresses(plan);

        if (_options.Updater.DryRun)
        {
            foreach (var operation in plan.Operations)
            {
                _logger.LogInformation("DryRun {Domain} {RecordType} {Operation}：{Reason}",
                    plan.Domain, operation.Type, operation.Kind, operation.Reason);
            }

            return;
        }

        foreach (var operation in plan.Operations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            switch (operation.Kind)
            {
                case DnsOperationKind.None:
                    _logger.LogInformation("{Domain} {RecordType}：{Reason}", plan.Domain, operation.Type, operation.Reason);
                    break;
                case DnsOperationKind.Skip:
                    _logger.LogWarning("{Domain} {RecordType}：{Reason}", plan.Domain, operation.Type, operation.Reason);
                    break;
                case DnsOperationKind.Add:
                    {
                        var address = IPAddress.Parse(operation.DesiredAddress!);
                        var added = await _dnsClient.AddAsync(plan.Target, operation.Type, address, cancellationToken);
                        AddChange(result, plan.Domain, operation, added.Value, null);
                        break;
                    }
                case DnsOperationKind.Update:
                    {
                        var address = IPAddress.Parse(operation.DesiredAddress!);
                        await _dnsClient.UpdateAsync(plan.Target, operation.ExistingRecord!, address, cancellationToken);
                        AddChange(result, plan.Domain, operation, address.ToString(), operation.ExistingRecord!.Value);
                        break;
                    }
                case DnsOperationKind.Delete:
                    if (_options.Updater.ConflictPolicy != ConflictPolicy.Replace)
                    {
                        throw new InvalidOperationException("执行层拒绝了未授权的删除操作。");
                    }

                    await _dnsClient.DeleteAsync(operation.ExistingRecord!, cancellationToken);
                    AddChange(result, plan.Domain, operation, null, operation.ExistingRecord!.Value);
                    break;
            }
        }
    }

    private async Task ProcessTargetAsync(
        DdnsTarget target,
        UpdateResult result,
        CancellationToken cancellationToken)
    {
        var provider = _ipProviders[target.Source];
        var addresses = new Dictionary<DnsRecordType, IPAddress?>();
        foreach (var type in target.Types.Distinct())
        {
            var address = await provider.GetAddressAsync(target, type, cancellationToken);
            addresses[type] = address;
            if (address is null)
            {
                _logger.LogWarning("目标 {Domain} 未获取到有效 {RecordType} 地址。", target.Domain, type);
            }
        }

        if (addresses.Values.All(address => address is null))
        {
            throw new InvalidOperationException("未获取到任何有效 IP，已跳过 DNS 查询和写操作。");
        }

        var resolved = await _zoneResolver.ResolveAsync(target, cancellationToken);
        var records = await _dnsClient.GetRecordsAsync(resolved, cancellationToken);
        var plan = _planner.Build(resolved, addresses, records, _options.Updater.ConflictPolicy);
        await ExecutePlanAsync(plan, result, cancellationToken);
    }

    private void ValidatePlanAuthorization(UpdatePlan plan)
    {
        if (_options.Updater.ConflictPolicy != ConflictPolicy.Replace &&
            plan.Operations.Any(operation => operation.Kind == DnsOperationKind.Delete))
        {
            throw new InvalidOperationException("执行层在写入前拒绝了未授权的删除计划。");
        }
    }

    private static void ValidatePlanAddresses(UpdatePlan plan)
    {
        foreach (var operation in plan.Operations.Where(operation =>
                     operation.Kind is DnsOperationKind.Add or DnsOperationKind.Update))
        {
            if (!IPAddress.TryParse(operation.DesiredAddress, out var address) ||
                !IpAddressValidator.HasExpectedFamily(address, operation.Type) ||
                (plan.Target.Target.Source == IpAddressSource.Public && !IpAddressValidator.IsPublic(address)) ||
                (plan.Target.Target.Source == IpAddressSource.Local && !IpAddressValidator.IsUsableLocal(address)))
            {
                throw new InvalidOperationException($"执行层拒绝了无效 {operation.Type} 地址。");
            }
        }
    }

    private void AddChange(
        UpdateResult result,
        string domain,
        DnsOperation operation,
        string? newAddress,
        string? oldAddress)
    {
        result.SuccessfulChanges.Add(new DnsChange
        {
            Domain = domain,
            RecordType = operation.Type.ToString(),
            Operation = operation.Kind.ToString(),
            OldAddress = oldAddress,
            NewAddress = newAddress
        });
        _logger.LogInformation("{Domain} {RecordType} {Operation} 成功，旧值 {OldAddress}，新值 {NewAddress}",
            domain, operation.Type, operation.Kind, oldAddress ?? "-", newAddress ?? "-");
    }
}
