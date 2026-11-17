using AliyunDdns.Application;
using AliyunDdns.Configuration;
using Microsoft.Extensions.Options;

namespace AliyunDdns.Hosting;

public sealed class DdnsWorker : BackgroundService
{
    private readonly DdnsUpdateService _updateService;
    private readonly DdnsOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DdnsWorker> _logger;

    public DdnsWorker(
        DdnsUpdateService updateService,
        IOptions<DdnsOptions> options,
        TimeProvider timeProvider,
        ILogger<DdnsWorker> logger)
    {
        _updateService = updateService;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.Updater.IntervalSeconds), _timeProvider);
        do
        {
            try
            {
                var result = await _updateService.RunOnceAsync(stoppingToken);
                _logger.LogInformation("本轮更新时间 {Timestamp}，成功变更 {ChangeCount} 项，失败目标 {FailureCount} 个。",
                    result.Timestamp, result.SuccessfulChanges.Count, result.Failures.Count);
                if (result.HasFailures)
                {
                    _logger.LogWarning("本轮更新完成，失败目标 {FailureCount} 个。", result.Failures.Count);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "本轮更新发生未处理异常。");
            }
        }
        while (await WaitForNextTickAsync(timer, stoppingToken));
    }

    internal static async Task<bool> WaitForNextTickAsync(
        PeriodicTimer timer,
        CancellationToken cancellationToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }
}
