namespace AliyunDdns.Configuration;

public static class SecretResolver
{
    public static string ResolveAccessKeySecret(DdnsOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Aliyun.AccessKeySecretFile))
        {
            return File.ReadAllText(options.Aliyun.AccessKeySecretFile).Trim();
        }

        return options.Aliyun.AccessKeySecret;
    }
}
