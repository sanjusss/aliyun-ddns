using System.Buffers.Binary;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using AliyunDdns.Configuration;
using AliyunDdns.Domain;
using AliyunDdns.Infrastructure.Ip;
using Xunit;

namespace AliyunDdns.Tests;

public sealed class LinuxNetlinkAddressMetadataSourceTests
{
    [Fact]
    public void Parser_ReadsTemporaryDeprecatedAndLifetimeMetadata()
    {
        using var message = new MemoryStream();
        message.Write(new byte[24]);
        message.GetBuffer()[16] = (byte)System.Net.Sockets.AddressFamily.InterNetworkV6;
        WriteAttribute(message, 1, IPAddress.Parse("240e::100").GetAddressBytes());
        WriteAttribute(message, 3, Encoding.UTF8.GetBytes("eth0\0"));
        var cacheInfo = new byte[16];
        BinaryPrimitives.WriteUInt32LittleEndian(cacheInfo.AsSpan(4, 4), 3600);
        WriteAttribute(message, 4, cacheInfo);
        var flags = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(flags, 0x21);
        WriteAttribute(message, 8, flags);
        var bytes = message.ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0, 4), bytes.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(4, 2), 20);
        var metadata = LinuxNetlinkAddressMetadataSource.ParseMetadata(bytes, out var completed);
        var value = Assert.Single(metadata).Value;
        Assert.False(completed);
        Assert.True(value.IsTemporary);
        Assert.True(value.IsDeprecated);
        Assert.Equal(SuffixOrigin.Random, value.SuffixOrigin);
    }

    [Fact]
    public void LinuxNetlink_CanReadLocalMetadata()
    {
        if (!OperatingSystem.IsLinux())
        {
            return;
        }

        var metadata = new LinuxNetlinkAddressMetadataSource().GetMetadata();
        Assert.NotNull(metadata);
    }

    [Fact]
    public async Task StablePolicy_UsesFakeNetlinkMetadata()
    {
        var temporary = Candidate("240e::100");
        var stable = Candidate("240e::200");
        var deprecated = Candidate("240e::300");
        var provider = CreateProvider([temporary, stable, deprecated], new Dictionary<LocalAddressKey, LocalAddressMetadata>
        {
            [Key(temporary)] = new() { IsTemporary = true, SuffixOrigin = SuffixOrigin.Random },
            [Key(deprecated)] = new() { IsDeprecated = true }
        });
        Assert.Equal(stable.Address, await provider.GetAddressAsync(LocalTarget(LocalAddressPolicy.Stable),
            DnsRecordType.AAAA, CancellationToken.None));
    }

    [Fact]
    public async Task Eui64Policy_DeprioritizesTemporaryFromFakeMetadata()
    {
        var temporary = Candidate("240e::211:22ff:fe33:4455");
        var stable = Candidate("240e::111:22ff:fe33:4455");
        var provider = CreateProvider([temporary, stable], new Dictionary<LocalAddressKey, LocalAddressMetadata>
        {
            [Key(temporary)] = new() { IsTemporary = true, SuffixOrigin = SuffixOrigin.Random }
        });
        Assert.Equal(stable.Address, await provider.GetAddressAsync(LocalTarget(LocalAddressPolicy.Eui64),
            DnsRecordType.AAAA, CancellationToken.None));
    }

    private static LocalIpAddressProvider CreateProvider(
        IEnumerable<LocalAddressCandidate> candidates,
        IReadOnlyDictionary<LocalAddressKey, LocalAddressMetadata> metadata)
    {
        var source = new NetworkInterfaceLocalAddressSource(new FakeInventory(candidates), new FakeMetadata(metadata));
        return new LocalIpAddressProvider(source);
    }

    private static DdnsTarget LocalTarget(LocalAddressPolicy policy)
    {
        var target = TestData.Target(source: IpAddressSource.Local);
        target.Types = [DnsRecordType.AAAA];
        target.AddressPolicy = policy;
        target.Networks = ["240e::/16"];
        return target;
    }

    private static LocalAddressCandidate Candidate(string address)
    {
        return new LocalAddressCandidate { Address = IPAddress.Parse(address), InterfaceName = "eth0" };
    }

    private static LocalAddressKey Key(LocalAddressCandidate candidate)
    {
        return new LocalAddressKey(candidate.InterfaceName, candidate.Address);
    }

    private static void WriteAttribute(Stream stream, ushort type, byte[] value)
    {
        var attribute = new byte[4 + value.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(attribute.AsSpan(0, 2), (ushort)attribute.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(attribute.AsSpan(2, 2), type);
        value.CopyTo(attribute, 4);
        stream.Write(attribute);
        stream.Write(new byte[(-attribute.Length) & 3]);
    }

    private sealed class FakeInventory(IEnumerable<LocalAddressCandidate> candidates) : INetworkInterfaceInventory
    {
        public IEnumerable<LocalAddressCandidate> GetCandidates() => candidates;
    }

    private sealed class FakeMetadata(IReadOnlyDictionary<LocalAddressKey, LocalAddressMetadata> metadata) : ILocalAddressMetadataSource
    {
        public IReadOnlyDictionary<LocalAddressKey, LocalAddressMetadata> GetMetadata() => metadata;
    }
}
