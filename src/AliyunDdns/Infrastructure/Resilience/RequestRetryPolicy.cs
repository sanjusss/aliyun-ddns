using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using AliyunDdns.Configuration;
using Microsoft.Extensions.Options;
using Tea;

namespace AliyunDdns.Infrastructure.Resilience;

public sealed class RequestRetryPolicy
{
    private readonly UpdaterOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RequestRetryPolicy> _logger;

    public RequestRetryPolicy(
        IOptions<DdnsOptions> options,
        TimeProvider timeProvider,
        ILogger<RequestRetryPolicy> logger)
    {
        _options = options.Value.Updater;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await operation(cancellationToken);
            }
            catch (Exception exception) when (attempt < _options.RetryMaxAttempts &&
                                               IsTransient(exception, cancellationToken))
            {
                var delay = TimeSpan.FromMilliseconds(
                    _options.RetryBaseDelayMilliseconds * Math.Pow(2, attempt - 1));
                _logger.LogWarning(
                    "请求暂时失败 {ExceptionType}，等待 {DelayMilliseconds} ms 后执行第 {NextAttempt} 次尝试，最多 {MaxAttempts} 次。",
                    exception.GetType().Name, delay.TotalMilliseconds, attempt + 1, _options.RetryMaxAttempts);
                await Task.Delay(delay, _timeProvider, cancellationToken);
            }
        }
    }

    internal static bool IsTransient(Exception exception, CancellationToken cancellationToken)
    {
        return exception switch
        {
            TeaRetryableException => true,
            TeaUnretryableException => false,
            TeaException teaException => (teaException.Code?.Contains("Throttling", StringComparison.OrdinalIgnoreCase) ?? false) ||
                                         IsTransientStatusCode(teaException.StatusCode) ||
                                         (teaException.InnerException is not null &&
                                          IsTransient(teaException.InnerException, cancellationToken)),
            HttpRequestException httpException => httpException.StatusCode is null ||
                                                  IsTransientStatusCode((int)httpException.StatusCode.Value) ||
                                                  (httpException.InnerException is not null &&
                                                   IsTransient(httpException.InnerException, cancellationToken)),
            OperationCanceledException when !cancellationToken.IsCancellationRequested => true,
            TimeoutException => true,
            IOException => true,
            SocketException => true,
            _ => false
        };
    }

    private static bool IsTransientStatusCode(int statusCode)
    {
        return statusCode == (int)HttpStatusCode.TooManyRequests || statusCode >= 500;
    }
}
