using AliyunDdns.Configuration;
using AliyunDdns.Domain;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AliyunDdns.Tests;

public sealed class DdnsOptionsBinderTests
{
    [Fact]
    public void MissingConflictPolicyAndTimeZone_PreservesLegacyDefaults()
    {
        var options = DdnsOptionsBinder.Bind(BuildConfiguration([]));
        Assert.Equal(ConflictPolicy.Replace, options.Updater.ConflictPolicy);
        Assert.Equal(8, options.Updater.LogTimeZoneOffsetHours);
    }

    [Fact]
    public void SingleTypeBinding_DoesNotAppendDefaultTypes()
    {
        var options = DdnsOptionsBinder.Bind(BuildConfiguration(new Dictionary<string, string?>
        {
            ["Targets:0:Domain"] = "home.example.com",
            ["Targets:0:RootDomain"] = "example.com",
            ["Targets:0:Types:0"] = "AAAA"
        }));
        Assert.Equal([DnsRecordType.AAAA], options.Targets.Single().Types);
    }

    [Fact]
    public void MissingTypesBinding_AppliesDualStackDefault()
    {
        var options = DdnsOptionsBinder.Bind(BuildConfiguration(new Dictionary<string, string?>
        {
            ["Targets:0:Domain"] = "home.example.com",
            ["Targets:0:RootDomain"] = "example.com"
        }));
        Assert.Equal([DnsRecordType.A, DnsRecordType.AAAA], options.Targets.Single().Types);
    }

    [Theory]
    [InlineData("Ttl", "invalid")]
    [InlineData("Source", "invalid")]
    public void InvalidTargetElement_ReportsFieldPathAndDoesNotDropTarget(string field, string value)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => DdnsOptionsBinder.Bind(BuildConfiguration(
            new Dictionary<string, string?>
            {
                ["Targets:0:Domain"] = "valid.example.com",
                ["Targets:0:RootDomain"] = "example.com",
                ["Targets:0:Types:0"] = "A",
                ["Targets:1:Domain"] = "invalid-value.example.com",
                ["Targets:1:RootDomain"] = "example.com",
                ["Targets:1:Types:0"] = "A",
                [$"Targets:1:{field}"] = value
            })));
        Assert.Contains($"Targets:1:{field}", exception.Message, StringComparison.Ordinal);
    }

    internal static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
