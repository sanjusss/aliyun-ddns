using System.Text;
using System.Text.Json;
using AliyunDdns.Application;
using AliyunDdns.Configuration;
using Microsoft.Extensions.Options;

namespace AliyunDdns.Infrastructure.Notifications;

public sealed class WebhookNotificationSink : INotificationSink
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DdnsOptions _options;

    public WebhookNotificationSink(IHttpClientFactory httpClientFactory, IOptions<DdnsOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task NotifyAsync(UpdateResult result, CancellationToken cancellationToken)
    {
        if (result.SuccessfulChanges.Count == 0 || string.IsNullOrWhiteSpace(_options.Webhook.Url))
        {
            return;
        }

        var text = new StringBuilder($"以下域名在 {result.Timestamp:O} 发生变化：");
        foreach (var change in result.SuccessfulChanges)
        {
            text.Append($"\n{change.Domain} ({change.RecordType}) {change.Operation}: {change.OldAddress ?? "-"} => {change.NewAddress ?? "-"}");
        }

        var payload = new
        {
            timestamp = result.Timestamp.ToUnixTimeSeconds(),
            changes = result.SuccessfulChanges.Select(change => new
            {
                domain = change.Domain,
                recordType = change.RecordType,
                operation = change.Operation,
                oldIp = change.OldAddress,
                ip = change.NewAddress
            }),
            msgtype = "text",
            text = new { content = text.ToString() }
        };

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(_options.Webhook.TimeoutSeconds));
        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await _httpClientFactory.CreateClient("webhook")
            .PostAsync(_options.Webhook.Url, content, timeout.Token);
        response.EnsureSuccessStatusCode();
    }
}
