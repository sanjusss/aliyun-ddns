namespace AliyunDdns.Configuration;

public sealed class DdnsOptions
{
    public UpdaterOptions Updater { get; set; } = new();

    public AliyunOptions Aliyun { get; set; } = new();

    public WebhookOptions Webhook { get; set; } = new();

    public List<DdnsTarget> Targets { get; set; } = [];
}

public sealed class UpdaterOptions
{
    public int IntervalSeconds { get; set; } = 300;

    public bool DryRun { get; set; }

    public ConflictPolicy ConflictPolicy { get; set; } = ConflictPolicy.Replace;

    public int RequestTimeoutSeconds { get; set; } = 10;

    public int RetryMaxAttempts { get; set; } = 3;

    public int RetryBaseDelayMilliseconds { get; set; } = 500;

    public double LogTimeZoneOffsetHours { get; set; } = 8;

    public bool ChinaIPv4Only { get; set; }
}

public sealed class AliyunOptions
{
    public string AccessKeyId { get; set; } = string.Empty;

    public string AccessKeySecret { get; set; } = string.Empty;

    public string AccessKeySecretFile { get; set; } = string.Empty;

    public string Endpoint { get; set; } = "alidns.aliyuncs.com";
}

public sealed class WebhookOptions
{
    public string Url { get; set; } = string.Empty;

    public int TimeoutSeconds { get; set; } = 10;
}
