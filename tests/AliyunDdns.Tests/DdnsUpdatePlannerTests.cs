using System.Net;
using AliyunDdns.Application;
using AliyunDdns.Configuration;
using AliyunDdns.Domain;
using Xunit;

namespace AliyunDdns.Tests;

public sealed class DdnsUpdatePlannerTests
{
    private readonly DdnsUpdatePlanner _planner = new();

    [Fact]
    public void NullAddress_SkipsWithoutChanges()
    {
        var plan = _planner.Build(TestData.Resolved(), new Dictionary<DnsRecordType, IPAddress?> { [DnsRecordType.A] = null }, [], ConflictPolicy.Replace);
        Assert.All(plan.Operations, operation => Assert.Equal(DnsOperationKind.Skip, operation.Kind));
        Assert.False(plan.HasChanges);
    }

    [Fact]
    public void WrongAddressFamily_SkipsWithoutChanges()
    {
        var plan = Build([TestData.Record()], IPAddress.Parse("2001:4860:4860::8888"));
        Assert.Equal(DnsOperationKind.Skip, Assert.Single(plan.Operations).Kind);
    }

    [Fact]
    public void NoRecord_Adds()
    {
        var operation = Assert.Single(Build([], IPAddress.Parse("8.8.8.8")).Operations);
        Assert.Equal(DnsOperationKind.Add, operation.Kind);
        Assert.Equal("8.8.8.8", operation.DesiredAddress);
    }

    [Fact]
    public void SameAddress_DoesNothing()
    {
        var operation = Assert.Single(Build([TestData.Record(value: "8.8.8.8")], IPAddress.Parse("8.8.8.8")).Operations);
        Assert.Equal(DnsOperationKind.None, operation.Kind);
    }

    [Fact]
    public void DifferentAddress_UpdatesExistingRecordId()
    {
        var operation = Assert.Single(Build([TestData.Record(id: "record-42")], IPAddress.Parse("8.8.8.8")).Operations);
        Assert.Equal(DnsOperationKind.Update, operation.Kind);
        Assert.Equal("record-42", operation.ExistingRecord!.RecordId);
    }

    [Fact]
    public void MultipleRecords_SkipWithoutDelete()
    {
        var plan = Build([TestData.Record(id: "1"), TestData.Record(id: "2")], IPAddress.Parse("8.8.8.8"));
        Assert.Equal(DnsOperationKind.Skip, Assert.Single(plan.Operations).Kind);
        Assert.DoesNotContain(plan.Operations, operation => operation.Kind == DnsOperationKind.Delete);
    }

    [Fact]
    public void MultipleRecords_ReplaceUpdatesPrimaryThenDeletesExtras()
    {
        var plan = _planner.Build(TestData.Resolved(), Address("8.8.8.8"),
            [TestData.Record(id: "2"), TestData.Record(id: "1"), TestData.Record(id: "3")], ConflictPolicy.Replace);
        Assert.Equal([DnsOperationKind.Update, DnsOperationKind.Delete, DnsOperationKind.Delete], plan.Operations.Select(operation => operation.Kind));
        Assert.Equal("1", plan.Operations[0].ExistingRecord!.RecordId);
        Assert.Equal(["2", "3"], plan.Operations.Skip(1).Select(operation => operation.ExistingRecord!.RecordId));
    }

    [Fact]
    public void Cname_Skip()
    {
        var plan = Build([TestData.Record(DnsRecordType.CNAME, "target.example.net")], IPAddress.Parse("8.8.8.8"));
        Assert.Equal(DnsOperationKind.Skip, Assert.Single(plan.Operations).Kind);
    }

    [Fact]
    public void Cname_ReplaceDeletesThenAdds()
    {
        var plan = _planner.Build(TestData.Resolved(), Address("8.8.8.8"),
            [TestData.Record(DnsRecordType.CNAME, "target.example.net")], ConflictPolicy.Replace);
        Assert.Equal([DnsOperationKind.Delete, DnsOperationKind.Add], plan.Operations.Select(operation => operation.Kind));
    }

    [Fact]
    public void CnameAndNullAddress_ReplaceStillDoesNotDelete()
    {
        var plan = _planner.Build(TestData.Resolved(), new Dictionary<DnsRecordType, IPAddress?> { [DnsRecordType.A] = null },
            [TestData.Record(DnsRecordType.CNAME, "target.example.net")], ConflictPolicy.Replace);
        Assert.Equal(DnsOperationKind.Skip, Assert.Single(plan.Operations).Kind);
    }

    [Fact]
    public void SameIpWithDifferentTtl_UpdatesByRecordId()
    {
        var target = TestData.Resolved();
        target.Target.Ttl = 300;
        var plan = _planner.Build(target, Address("8.8.8.8"),
            [TestData.Record(value: "8.8.8.8", ttl: 600)], ConflictPolicy.Skip);
        var operation = Assert.Single(plan.Operations);
        Assert.Equal(DnsOperationKind.Update, operation.Kind);
        Assert.Equal("1", operation.ExistingRecord?.RecordId);
    }

    private UpdatePlan Build(IReadOnlyList<DnsRecord> records, IPAddress address)
    {
        return _planner.Build(TestData.Resolved(), Address(address.ToString()), records, ConflictPolicy.Skip);
    }

    private static IReadOnlyDictionary<DnsRecordType, IPAddress?> Address(string address)
    {
        return new Dictionary<DnsRecordType, IPAddress?> { [DnsRecordType.A] = IPAddress.Parse(address) };
    }
}
