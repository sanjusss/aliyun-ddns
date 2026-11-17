using AliyunDdns.Application;
using AliyunDdns.Domain;

namespace AliyunDdns.Infrastructure.Notifications;

public sealed class NullNotificationSink : INotificationSink
{
    public Task NotifyAsync(UpdateResult result, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
