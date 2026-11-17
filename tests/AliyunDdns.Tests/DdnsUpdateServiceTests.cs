using System.Net;
using AliyunDdns.Application;
using AliyunDdns.Configuration;
using AliyunDdns.Domain;
using AliyunDdns.Infrastructure.Aliyun;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AliyunDdns.Tests;

public sealed class DdnsUpdateServiceTests
{
    [Fact]
    public async Task DryRun_DoesNotWriteOrNotify()
    {
        var options = TestData.Options(TestData.Target());
        options.Updater.DryRun = true;
        options.Updater.ConflictPolicy = ConflictPolicy.Replace;
        var dns = new FakeDnsClient();
        dns.Records["home.example.com"] = [TestData.Record(DnsRecordType.CNAME, "target.example.net")];
        var notification = new FakeNotificationSink();
        var service = Create(options, dns, IPAddress.Parse("8.8.8.8"), notification);
        var result = await service.RunOnceAsync(CancellationToken.None);
        Assert.Empty(dns.Added);
        Assert.Empty(dns.Updated);
        Assert.Empty(dns.Deleted);
        Assert.Empty(result.SuccessfulChanges);
        Assert.Equal(0, notification.CallCount);
    }

    [Fact]
    public async Task Executor_RejectsUnauthorizedDeleteBeforeAnyWrite()
    {
        var options = TestData.Options(TestData.Target());
        options.Updater.ConflictPolicy = ConflictPolicy.Skip;
        var service = Create(options, new FakeDnsClient(), IPAddress.Parse("8.8.8.8"), new FakeNotificationSink());
        var plan = new UpdatePlan
        {
            Target = TestData.Resolved(),
            Operations = [new DnsOperation { Kind = DnsOperationKind.Delete, Type = DnsRecordType.A, ExistingRecord = TestData.Record(), Reason = "test" }]
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecutePlanAsync(plan, new UpdateResult(), CancellationToken.None));
    }

    [Fact]
    public async Task Executor_RejectsInvalidAddressBeforeAnyWrite()
    {
        var dns = new FakeDnsClient();
        var service = Create(TestData.Options(TestData.Target()), dns, IPAddress.Parse("8.8.8.8"), new FakeNotificationSink());
        var plan = new UpdatePlan
        {
            Target = TestData.Resolved(),
            Operations = [new DnsOperation { Kind = DnsOperationKind.Add, Type = DnsRecordType.A, DesiredAddress = "127.0.0.1", Reason = "test" }]
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecutePlanAsync(plan, new UpdateResult(), CancellationToken.None));
        Assert.Empty(dns.Added);
    }

    [Fact]
    public async Task FullFlow_OneTargetFailureDoesNotBlockOtherTarget()
    {
        var bad = TestData.Target("bad.example.com");
        var good = TestData.Target("home.example.com");
        var options = TestData.Options(bad, good);
        var dns = new FakeDnsClient();
        dns.FailingDomains.Add(bad.Domain);
        dns.Records[good.Domain] = [TestData.Record(value: "1.1.1.1")];
        var notification = new FakeNotificationSink();
        var result = await Create(options, dns, IPAddress.Parse("8.8.8.8"), notification).RunOnceAsync(CancellationToken.None);
        Assert.Single(result.Failures);
        Assert.Equal("bad.example.com", result.Failures[0].Domain);
        Assert.Single(dns.Updated);
        Assert.Equal("8.8.8.8", dns.Updated[0].Value);
        Assert.Equal(1, notification.CallCount);
        Assert.Single(notification.Result!.SuccessfulChanges);
    }

    [Fact]
    public async Task NullIp_SkipsDnsQueryAndWrite()
    {
        var dns = new FakeDnsClient();
        var notification = new FakeNotificationSink();
        var result = await Create(TestData.Options(TestData.Target()), dns, null, notification).RunOnceAsync(CancellationToken.None);
        Assert.Single(result.Failures);
        Assert.Equal(0, dns.GetRecordsCallCount);
        Assert.Empty(dns.Added);
        Assert.Empty(dns.Updated);
        Assert.Empty(dns.Deleted);
        Assert.Equal(0, notification.CallCount);
    }

    [Fact]
    public async Task DefaultReplace_UpdatesPrimaryAndDeletesDuplicate()
    {
        var options = TestData.Options(TestData.Target());
        var dns = new FakeDnsClient();
        dns.Records["home.example.com"] = [TestData.Record(id: "1"), TestData.Record(id: "2")];
        var result = await Create(options, dns, IPAddress.Parse("8.8.8.8"), new FakeNotificationSink()).RunOnceAsync(CancellationToken.None);
        Assert.Single(dns.Updated);
        Assert.Equal("2", Assert.Single(dns.Deleted).RecordId);
        Assert.Equal(2, result.SuccessfulChanges.Count);
    }

    [Fact]
    public async Task UnchangedRecord_DoesNotNotify()
    {
        var dns = new FakeDnsClient();
        dns.Records["home.example.com"] = [TestData.Record(value: "8.8.8.8")];
        var notification = new FakeNotificationSink();
        var result = await Create(TestData.Options(TestData.Target()), dns, IPAddress.Parse("8.8.8.8"), notification).RunOnceAsync(CancellationToken.None);
        Assert.Empty(result.SuccessfulChanges);
        Assert.Equal(0, notification.CallCount);
        Assert.Empty(dns.Updated);
    }

    [Fact]
    public async Task Cancellation_StopsBeforeProcessingTargets()
    {
        var dns = new FakeDnsClient();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var service = Create(TestData.Options(TestData.Target()), dns, IPAddress.Parse("8.8.8.8"), new FakeNotificationSink());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.RunOnceAsync(cancellation.Token));
        Assert.Equal(0, dns.GetRecordsCallCount);
    }

    [Fact]
    public async Task TrailingDotTarget_UsesNormalizedQueryAndDoesNotAddDuplicate()
    {
        var options = TestData.Options(TestData.Target("Home.Example.COM."));
        var dns = new FakeDnsClient();
        dns.Records["home.example.com"] = [TestData.Record(value: "8.8.8.8")];
        await Create(options, dns, IPAddress.Parse("8.8.8.8"), new FakeNotificationSink())
            .RunOnceAsync(CancellationToken.None);
        Assert.Empty(dns.Added);
        Assert.Empty(dns.Updated);
    }

    [Fact]
    public async Task BoundSingleType_DoesNotWriteOtherType()
    {
        var options = DdnsOptionsBinder.Bind(DdnsOptionsBinderTests.BuildConfiguration(new Dictionary<string, string?>
        {
            ["Targets:0:Domain"] = "home.example.com",
            ["Targets:0:RootDomain"] = "example.com",
            ["Targets:0:Types:0"] = "AAAA"
        }));
        var dns = new FakeDnsClient();
        await Create(options, dns, IPAddress.Parse("240e::1"), new FakeNotificationSink())
            .RunOnceAsync(CancellationToken.None);
        Assert.Equal(DnsRecordType.AAAA, Assert.Single(dns.Added).Type);
        Assert.DoesNotContain(dns.Updated, record => record.Type == DnsRecordType.A);
    }

    private static DdnsUpdateService Create(
        DdnsOptions options,
        FakeDnsClient dns,
        IPAddress? address,
        FakeNotificationSink notification)
    {
        var provider = new FakeIpProvider(IpAddressSource.Public, (_, _) => address);
        return new DdnsUpdateService([provider], dns, new AliyunZoneResolver(dns), new DdnsUpdatePlanner(), notification,
            Options.Create(options), TimeProvider.System, NullLogger<DdnsUpdateService>.Instance);
    }
}
