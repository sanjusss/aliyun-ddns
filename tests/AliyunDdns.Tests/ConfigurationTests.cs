using AliyunDdns.Configuration;
using AliyunDdns.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AliyunDdns.Tests;

public sealed class ConfigurationTests
{
    [Fact]
    public void Defaults_MatchLegacyConflictPolicyAndTimeZone()
    {
        var options = new DdnsOptions();
        Assert.Equal(ConflictPolicy.Replace, options.Updater.ConflictPolicy);
        Assert.Equal(8, options.Updater.LogTimeZoneOffsetHours);
    }

    [Fact]
    public void LegacyEnvironment_ConvertsAllSupportedValues()
    {
        var environment = new Dictionary<string, string?>
        {
            ["AKID"] = "legacy-id",
            ["AKSCT"] = "legacy-secret",
            ["DOMAIN"] = "telecom:home.example.com,office.example.com",
            ["ROOT_DOMAIN"] = "example.com",
            ["REDO"] = "120",
            ["TTL"] = "300",
            ["TIMEZONE"] = "8.5",
            ["TYPE"] = "A, AAAA",
            ["CNIPV4"] = "true",
            ["WEBHOOK"] = "https://hook.example.test/path",
            ["CHECKLOCAL"] = "true",
            ["IPV4NETS"] = "192.168.1.0/24",
            ["IPV6NETS"] = "240e::/16"
        };
        var options = new DdnsOptions();
        var errors = new LegacyOptionsReader().Apply(options, EmptyConfiguration(), NullLogger.Instance, name => environment.GetValueOrDefault(name));
        Assert.Empty(errors);
        Assert.Equal("legacy-id", options.Aliyun.AccessKeyId);
        Assert.Equal("legacy-secret", options.Aliyun.AccessKeySecret);
        Assert.Equal(120, options.Updater.IntervalSeconds);
        Assert.Equal(8.5, options.Updater.LogTimeZoneOffsetHours);
        Assert.True(options.Updater.ChinaIPv4Only);
        Assert.Equal("https://hook.example.test/path", options.Webhook.Url);
        Assert.Equal(2, options.Targets.Count);
        Assert.Equal("telecom", options.Targets[0].Line);
        Assert.Equal("home.example.com", options.Targets[0].Domain);
        Assert.Equal([DnsRecordType.A, DnsRecordType.AAAA], options.Targets[0].Types);
        Assert.Equal(300, options.Targets[0].Ttl);
        Assert.Equal(IpAddressSource.Local, options.Targets[0].Source);
        Assert.Equal(["192.168.1.0/24", "240e::/16"], options.Targets[0].Networks);
        Assert.Empty(OptionsValidator.Validate(options));
    }

    [Fact]
    public void LegacyEnvironmentWithoutConflictPolicyOrTimeZone_UsesLegacyDefaults()
    {
        var environment = new Dictionary<string, string?>
        {
            ["AKID"] = "legacy-id",
            ["AKSCT"] = "legacy-secret",
            ["DOMAIN"] = "home.example.com",
            ["ROOT_DOMAIN"] = "example.com"
        };
        var options = new DdnsOptions();
        var errors = new LegacyOptionsReader().Apply(options, EmptyConfiguration(), NullLogger.Instance,
            name => environment.GetValueOrDefault(name));
        Assert.Empty(errors);
        Assert.Equal(ConflictPolicy.Replace, options.Updater.ConflictPolicy);
        Assert.Equal(8, options.Updater.LogTimeZoneOffsetHours);
    }

    [Fact]
    public void LegacyValues_DoNotOverrideExplicitNewConfiguration()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Aliyun:AccessKeyId"] = "new-id",
            ["Targets:0:Domain"] = "new.example.com"
        }).Build();
        var options = new DdnsOptions();
        configuration.Bind(options);
        new LegacyOptionsReader().Apply(options, configuration, NullLogger.Instance,
            name => new Dictionary<string, string?> { ["AKID"] = "old", ["DOMAIN"] = "old.example.com" }.GetValueOrDefault(name));
        Assert.Equal("new-id", options.Aliyun.AccessKeyId);
        Assert.Equal("new.example.com", Assert.Single(options.Targets).Domain);
    }

    [Fact]
    public void LegacyValues_CanFillBlankDefaultJsonValues()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Aliyun:AccessKeyId"] = "",
            ["Aliyun:AccessKeySecret"] = ""
        }).Build();
        var options = new DdnsOptions();
        configuration.Bind(options);
        new LegacyOptionsReader().Apply(options, configuration, NullLogger.Instance,
            name => new Dictionary<string, string?> { ["AKID"] = "legacy-id", ["AKSCT"] = "legacy-secret" }.GetValueOrDefault(name));
        Assert.Equal("legacy-id", options.Aliyun.AccessKeyId);
        Assert.Equal("legacy-secret", options.Aliyun.AccessKeySecret);
    }

    [Fact]
    public void InvalidLegacyNumbers_ReturnErrors()
    {
        var options = new DdnsOptions();
        var errors = new LegacyOptionsReader().Apply(options, EmptyConfiguration(), NullLogger.Instance,
            name => name == "REDO" ? "zero" : null);
        Assert.Contains(errors, error => error.Contains("REDO", StringComparison.Ordinal));
    }

    [Fact]
    public void Validator_RejectsInvalidConfiguration()
    {
        var options = new DdnsOptions();
        options.Updater.IntervalSeconds = 0;
        options.Updater.RetryMaxAttempts = 0;
        options.Updater.RetryBaseDelayMilliseconds = -1;
        var errors = OptionsValidator.Validate(options);
        Assert.Contains(errors, error => error.Contains("IntervalSeconds", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("RetryMaxAttempts", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("RetryBaseDelayMilliseconds", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("AccessKeyId", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Contains("至少", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(8.01, false)]
    [InlineData(8.5, true)]
    public void TimeZoneOffset_MustRepresentWholeMinutes(double offset, bool valid)
    {
        var options = TestData.Options();
        options.Updater.LogTimeZoneOffsetHours = offset;
        var errors = OptionsValidator.Validate(options);
        Assert.Equal(valid, !errors.Any(error => error.Contains("整分钟", StringComparison.Ordinal)));
    }

    [Fact]
    public void WildcardTarget_IsAllowedOnlyAtLeftmostLabel()
    {
        var wildcard = TestData.Options(TestData.Target("*.example.com"));
        Assert.Empty(OptionsValidator.Validate(wildcard));
        var invalidRoot = TestData.Options(TestData.Target("home.example.com"));
        invalidRoot.Targets[0].RootDomain = "*.example.com";
        Assert.Contains(OptionsValidator.Validate(invalidRoot), error => error.Contains("RootDomain", StringComparison.Ordinal));
        var invalidWildcard = TestData.Options(TestData.Target("home.*.example.com"));
        Assert.Contains(OptionsValidator.Validate(invalidWildcard), error => error.Contains("Domain", StringComparison.Ordinal));
    }

    [Fact]
    public void LegacyLocalCommandLineWithoutNetworks_UsesAutomaticFiltering()
    {
        var parsed = CommandLineOptions.Parse([
            "-u", "legacy-id", "-p", "legacy-secret", "-d", "home.example.com",
            "--root-domain", "example.com", "-c"
        ]);
        var options = new DdnsOptions();
        new LegacyOptionsReader().Apply(options, EmptyConfiguration(), NullLogger.Instance,
            name => parsed.LegacyValues.TryGetValue(name, out var value) ? value : null);
        Assert.Empty(OptionsValidator.Validate(options));
        Assert.Equal(IpAddressSource.Local, Assert.Single(options.Targets).Source);
        Assert.Empty(options.Targets[0].Networks);
        Assert.False(options.Targets[0].AllowAnyInterface);
    }

    [Fact]
    public void LegacyLocalEnvironmentWithoutNetworks_UsesAutomaticFiltering()
    {
        var environment = new Dictionary<string, string?>
        {
            ["AKID"] = "legacy-id",
            ["AKSCT"] = "legacy-secret",
            ["DOMAIN"] = "home.example.com",
            ["ROOT_DOMAIN"] = "example.com",
            ["CHECKLOCAL"] = "true"
        };
        var options = new DdnsOptions();
        new LegacyOptionsReader().Apply(options, EmptyConfiguration(), NullLogger.Instance,
            name => environment.GetValueOrDefault(name));
        Assert.Empty(OptionsValidator.Validate(options));
        Assert.Equal(IpAddressSource.Local, Assert.Single(options.Targets).Source);
        Assert.Empty(options.Targets[0].Networks);
        Assert.False(options.Targets[0].AllowAnyInterface);
    }

    [Fact]
    public void CommandLine_ParsesActionsAndConfiguration()
    {
        var result = CommandLineOptions.Parse(["--once", "--dry-run", "--validate-config", "--config", "test.json", "--Updater:IntervalSeconds=60"]);
        Assert.True(result.RunOnce);
        Assert.True(result.DryRun);
        Assert.True(result.ValidateOnly);
        Assert.Equal("test.json", result.ConfigPath);
        Assert.True(result.ConfigSpecified);
        Assert.Equal("--Updater:IntervalSeconds=60", Assert.Single(result.ConfigurationArguments));
    }

    [Fact]
    public void CommandLine_EqualsFormAndBooleanValues_AreParsed()
    {
        var result = CommandLineOptions.Parse([
            "--domain=home.example.com", "--type=AAAA", "--config=relative.json",
            "--dry-run=true", "--once=false", "--cnipv4=false", "--Updater:IntervalSeconds=60"
        ]);
        Assert.Equal("home.example.com", result.LegacyValues["DOMAIN"]);
        Assert.Equal("AAAA", result.LegacyValues["TYPE"]);
        Assert.Equal("relative.json", result.ConfigPath);
        Assert.True(result.DryRun);
        Assert.False(result.RunOnce);
        Assert.Equal("false", result.LegacyValues["CNIPV4"]);
        Assert.Equal("--Updater:IntervalSeconds=60", Assert.Single(result.ConfigurationArguments));
    }

    [Theory]
    [InlineData("--unknown")]
    [InlineData("--dry-run=invalid")]
    [InlineData("positional")]
    public void CommandLine_UnknownOrInvalidArguments_AreRejected(string argument)
    {
        Assert.Throws<ArgumentException>(() => CommandLineOptions.Parse([argument]));
    }

    [Fact]
    public void ExplicitRelativeConfig_UsesCurrentWorkingDirectory()
    {
        var parsed = CommandLineOptions.Parse(["--config=relative.json"]);
        Assert.Equal(Path.GetFullPath("relative.json", "/work/current"),
            parsed.GetConfigPath("/application", "/work/current"));
        var defaultConfig = CommandLineOptions.Parse([]);
        Assert.Equal(Path.GetFullPath("appsettings.json", "/application"),
            defaultConfig.GetConfigPath("/application", "/work/current"));
    }

    [Fact]
    public void LegacyCommandLine_IsConvertedAndMarkedForRemoval()
    {
        var parsed = CommandLineOptions.Parse([
            "-u", "legacy-id", "-p", "legacy-secret", "-d", "telecom:home.example.com",
            "--root-domain", "example.com", "-i", "120", "-t", "300", "--timezone", "8",
            "--type", "A,AAAA", "--cnipv4", "-h", "https://hook.example.test/", "-c",
            "--ipv4nets", "192.168.1.0/24", "--ipv46nets", "240e::/16"
        ]);
        var options = new DdnsOptions();
        var errors = new LegacyOptionsReader().Apply(options, EmptyConfiguration(), NullLogger.Instance,
            name => parsed.LegacyValues.TryGetValue(name, out var value) ? value : null);
        Assert.Empty(errors);
        Assert.Equal("legacy-id", options.Aliyun.AccessKeyId);
        Assert.Equal("legacy-secret", options.Aliyun.AccessKeySecret);
        Assert.Equal(120, options.Updater.IntervalSeconds);
        Assert.Equal("telecom", Assert.Single(options.Targets).Line);
        Assert.Equal(IpAddressSource.Local, options.Targets[0].Source);
        Assert.Equal("https://hook.example.test/", options.Webhook.Url);
        Assert.True(options.Updater.ChinaIPv4Only);
        Assert.False(parsed.ShowHelp);
        Assert.Empty(OptionsValidator.Validate(options));
    }

    [Fact]
    public void LegacyCommandLine_MissingValueIsRejected()
    {
        Assert.Throws<ArgumentException>(() => CommandLineOptions.Parse(["-u"]));
        Assert.True(CommandLineOptions.Parse(["--help"]).ShowHelp);
    }

    [Fact]
    public void SecretResolver_ReadsSecretFile()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "file-secret\n");
            var options = TestData.Options();
            options.Aliyun.AccessKeySecretFile = path;
            Assert.Equal("file-secret", SecretResolver.ResolveAccessKeySecret(options));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static IConfiguration EmptyConfiguration() => new ConfigurationBuilder().Build();
}
