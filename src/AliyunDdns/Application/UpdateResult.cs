namespace AliyunDdns.Application;

public sealed record DnsChange
{
    public required string Domain { get; init; }

    public required string RecordType { get; init; }

    public required string Operation { get; init; }

    public string? OldAddress { get; init; }

    public string? NewAddress { get; init; }
}

public sealed record TargetFailure
{
    public required string Domain { get; init; }

    public required string Error { get; init; }
}

public sealed class UpdateResult
{
    public DateTimeOffset Timestamp { get; set; }

    public IList<DnsChange> SuccessfulChanges { get; } = new List<DnsChange>();

    public IList<TargetFailure> Failures { get; } = new List<TargetFailure>();

    public bool NotificationFailed { get; set; }

    public bool HasFailures => Failures.Count > 0 || NotificationFailed;
}

public interface INotificationSink
{
    Task NotifyAsync(UpdateResult result, CancellationToken cancellationToken);
}
