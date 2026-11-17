using System.Net;
using AliyunDdns.Infrastructure.Resilience;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Tea;
using Xunit;

namespace AliyunDdns.Tests;

public sealed class RequestRetryPolicyTests
{
    [Fact]
    public async Task TooManyRequestsThenSuccess_RetriesAfterDelay()
    {
        var time = new FakeTimeProvider();
        var policy = CreatePolicy(time);
        var attempts = 0;
        var executing = policy.ExecuteAsync(_ =>
        {
            attempts++;
            return attempts == 1
                ? throw StatusCodeException(HttpStatusCode.TooManyRequests)
                : Task.FromResult(42);
        }, CancellationToken.None);
        Assert.False(executing.IsCompleted);
        time.Advance(TimeSpan.FromMilliseconds(100));
        Assert.Equal(42, await executing);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task ServerError_StopsAfterMaximumAttempts()
    {
        var time = new FakeTimeProvider();
        var policy = CreatePolicy(time);
        var attempts = 0;
        var executing = policy.ExecuteAsync<int>(_ =>
        {
            attempts++;
            throw StatusCodeException(HttpStatusCode.InternalServerError);
        }, CancellationToken.None);
        time.Advance(TimeSpan.FromMilliseconds(100));
        time.Advance(TimeSpan.FromMilliseconds(200));
        await Assert.ThrowsAsync<HttpRequestException>(() => executing);
        Assert.Equal(3, attempts);
    }

    [Fact]
    public async Task ClientError_DoesNotRetry()
    {
        var policy = CreatePolicy(new FakeTimeProvider());
        var attempts = 0;
        await Assert.ThrowsAsync<HttpRequestException>(() => policy.ExecuteAsync<int>(_ =>
        {
            attempts++;
            throw StatusCodeException(HttpStatusCode.BadRequest);
        }, CancellationToken.None));
        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task Cancellation_InterruptsBackoffWithoutWaiting()
    {
        var policy = CreatePolicy(new FakeTimeProvider());
        using var cancellation = new CancellationTokenSource();
        var attempts = 0;
        var executing = policy.ExecuteAsync<int>(_ =>
        {
            attempts++;
            throw StatusCodeException(HttpStatusCode.TooManyRequests);
        }, cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => executing);
        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task NetworkException_CanRetry()
    {
        var time = new FakeTimeProvider();
        var policy = CreatePolicy(time);
        var attempts = 0;
        var executing = policy.ExecuteAsync(_ =>
        {
            attempts++;
            return attempts == 1 ? throw new IOException("模拟网络中断") : Task.FromResult(7);
        }, CancellationToken.None);
        time.Advance(TimeSpan.FromMilliseconds(100));
        Assert.Equal(7, await executing);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task AliyunThrottlingWithHttp400_Retries()
    {
        var time = new FakeTimeProvider();
        var policy = CreatePolicy(time);
        var attempts = 0;
        var executing = policy.ExecuteAsync(_ =>
        {
            attempts++;
            return attempts == 1 ? throw TeaError("Throttling.User") : Task.FromResult(9);
        }, CancellationToken.None);
        time.Advance(TimeSpan.FromMilliseconds(100));
        Assert.Equal(9, await executing);
        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task OtherAliyunHttp400_DoesNotRetry()
    {
        var policy = CreatePolicy(new FakeTimeProvider());
        var attempts = 0;
        await Assert.ThrowsAsync<TeaException>(() => policy.ExecuteAsync<int>(_ =>
        {
            attempts++;
            throw TeaError("InvalidParameter");
        }, CancellationToken.None));
        Assert.Equal(1, attempts);
    }

    private static RequestRetryPolicy CreatePolicy(FakeTimeProvider time)
    {
        var options = TestData.Options();
        options.Updater.RetryMaxAttempts = 3;
        options.Updater.RetryBaseDelayMilliseconds = 100;
        return new RequestRetryPolicy(Options.Create(options), time, NullLogger<RequestRetryPolicy>.Instance);
    }

    private static TeaException TeaError(string code)
    {
        return new TeaException(new Dictionary<string, object>
        {
            ["code"] = code,
            ["statusCode"] = 400,
            ["message"] = "模拟阿里云错误"
        });
    }

    private static HttpRequestException StatusCodeException(HttpStatusCode statusCode)
    {
        return new HttpRequestException($"模拟 HTTP {(int)statusCode}", null, statusCode);
    }
}
