using AliyunDdns.Hosting;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace AliyunDdns.Tests;

public sealed class DdnsWorkerTests
{
    [Fact]
    public async Task PeriodicTimer_UsesInjectableTimeProvider()
    {
        var time = new FakeTimeProvider();
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30), time);
        var waiting = DdnsWorker.WaitForNextTickAsync(timer, CancellationToken.None);
        Assert.False(waiting.IsCompleted);
        time.Advance(TimeSpan.FromSeconds(30));
        Assert.True(await waiting);
    }

    [Fact]
    public async Task Cancellation_EndsTimerWait()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        using var cancellation = new CancellationTokenSource();
        var waiting = DdnsWorker.WaitForNextTickAsync(timer, cancellation.Token);
        cancellation.Cancel();
        Assert.False(await waiting);
    }
}
