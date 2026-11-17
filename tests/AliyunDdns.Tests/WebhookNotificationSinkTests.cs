using System.Net;
using AliyunDdns.Application;
using AliyunDdns.Infrastructure.Notifications;
using Microsoft.Extensions.Options;
using Xunit;

namespace AliyunDdns.Tests;

public sealed class WebhookNotificationSinkTests
{
    [Fact]
    public async Task SuccessfulChange_PostsExpectedPayload()
    {
        string? body = null;
        var handler = new FakeHttpMessageHandler((request, _) =>
        {
            body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return Task.FromResult(HttpResponses.Text("ok"));
        });
        var options = TestData.Options();
        options.Webhook.Url = "https://hook.example.test/secret-token";
        var sink = new WebhookNotificationSink(new FakeHttpClientFactory(handler), Options.Create(options));
        await sink.NotifyAsync(ResultWithChange(), CancellationToken.None);
        Assert.Single(handler.Requests);
        Assert.Contains("\"msgtype\":\"text\"", body, StringComparison.Ordinal);
        Assert.Contains("home.example.com", body, StringComparison.Ordinal);
        Assert.Contains("\"ip\":\"8.8.8.8\"", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NoChanges_DoesNotSendRequest()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(HttpResponses.Text("ok")));
        var options = TestData.Options();
        options.Webhook.Url = "https://hook.example.test/";
        await new WebhookNotificationSink(new FakeHttpClientFactory(handler), Options.Create(options))
            .NotifyAsync(new UpdateResult(), CancellationToken.None);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task HttpFailure_Throws()
    {
        var handler = new FakeHttpMessageHandler((_, _) => Task.FromResult(HttpResponses.Text("failed", HttpStatusCode.InternalServerError)));
        var options = TestData.Options();
        options.Webhook.Url = "https://hook.example.test/";
        var sink = new WebhookNotificationSink(new FakeHttpClientFactory(handler), Options.Create(options));
        await Assert.ThrowsAsync<HttpRequestException>(() => sink.NotifyAsync(ResultWithChange(), CancellationToken.None));
    }

    private static UpdateResult ResultWithChange()
    {
        var result = new UpdateResult { Timestamp = DateTimeOffset.UnixEpoch };
        result.SuccessfulChanges.Add(new DnsChange
        {
            Domain = "home.example.com",
            RecordType = "A",
            Operation = "Update",
            OldAddress = "1.1.1.1",
            NewAddress = "8.8.8.8"
        });
        return result;
    }
}
