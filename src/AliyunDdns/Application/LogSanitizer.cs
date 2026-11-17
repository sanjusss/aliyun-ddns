using AliyunDdns.Configuration;

namespace AliyunDdns.Application;

public static class LogSanitizer
{
    public static string Sanitize(string message, DdnsOptions options)
    {
        foreach (var secret in new[]
                 {
                     options.Aliyun.AccessKeySecret,
                     options.Aliyun.AccessKeyId,
                     options.Webhook.Url
                 }.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            message = message.Replace(secret, "***", StringComparison.Ordinal);
        }

        return message;
    }
}
