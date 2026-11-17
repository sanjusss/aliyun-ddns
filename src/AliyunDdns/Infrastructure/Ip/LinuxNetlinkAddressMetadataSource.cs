using System.Buffers.Binary;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace AliyunDdns.Infrastructure.Ip;

public sealed class LinuxNetlinkAddressMetadataSource : ILocalAddressMetadataSource
{
    private const int NetlinkRoute = 0;
    private const int NlMsgError = 2;
    private const int NlMsgDone = 3;
    private const int RtmNewAddress = 20;
    private const int RtmGetAddress = 22;
    private const int NlmFRequest = 0x01;
    private const int NlmFDump = 0x300;
    private const int IfaAddress = 1;
    private const int IfaLabel = 3;
    private const int IfaCacheInfo = 4;
    private const int IfaFlags = 8;
    private const uint IfaFTemporary = 0x01;
    private const uint IfaFDeprecated = 0x20;

    public IReadOnlyDictionary<LocalAddressKey, LocalAddressMetadata> GetMetadata()
    {
        if (!OperatingSystem.IsLinux())
        {
            return new Dictionary<LocalAddressKey, LocalAddressMetadata>();
        }

        try
        {
            return ReadMetadata();
        }
        catch (Exception exception) when (exception is IOException or PlatformNotSupportedException)
        {
            throw new InvalidOperationException("无法通过 Linux Netlink 读取本地 IPv6 地址元数据。", exception);
        }
    }

    private static IReadOnlyDictionary<LocalAddressKey, LocalAddressMetadata> ReadMetadata()
    {
        var descriptor = OpenSocket();
        try
        {
            var bindAddress = new SockAddrNl { Family = 16 };
            var kernelAddress = new SockAddrNl { Family = 16 };
            if (Bind(descriptor, ref bindAddress, 12) != 0)
            {
                throw LastError("绑定 Netlink 套接字失败");
            }

            var request = BuildRequest();
            if (SendTo(descriptor, request, (nuint)request.Length, 0, ref kernelAddress, 12) != request.Length)
            {
                throw LastError("发送 Netlink 地址查询失败");
            }

            var result = new Dictionary<LocalAddressKey, LocalAddressMetadata>();
            var buffer = new byte[65536];
            var completed = false;
            while (!completed)
            {
                var pollDescriptors = new[] { new PollFd { Descriptor = descriptor, Events = 1 } };
                if (Poll(pollDescriptors, 1, 2000) <= 0)
                {
                    throw new IOException("等待 Netlink 地址响应超时或失败。");
                }

                var length = Receive(descriptor, buffer, (nuint)buffer.Length, 0);
                if (length < 0)
                {
                    throw LastError("接收 Netlink 地址响应失败");
                }

                foreach (var item in ParseMetadata(buffer.AsSpan(0, (int)length), out completed))
                {
                    result[item.Key] = item.Value;
                }
            }

            return result;
        }
        finally
        {
            CloseSocket(descriptor);
        }
    }

    private static byte[] BuildRequest()
    {
        var request = new byte[28];
        BinaryPrimitives.WriteInt32LittleEndian(request.AsSpan(0, 4), request.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(request.AsSpan(4, 2), RtmGetAddress);
        BinaryPrimitives.WriteUInt16LittleEndian(request.AsSpan(6, 2), NlmFRequest | NlmFDump);
        BinaryPrimitives.WriteInt32LittleEndian(request.AsSpan(8, 4), 1);
        return request;
    }

    internal static IReadOnlyDictionary<LocalAddressKey, LocalAddressMetadata> ParseMetadata(
        ReadOnlySpan<byte> data,
        out bool completed)
    {
        completed = false;
        var result = new Dictionary<LocalAddressKey, LocalAddressMetadata>();
        for (var offset = 0; offset + 16 <= data.Length;)
        {
            var messageLength = BinaryPrimitives.ReadInt32LittleEndian(data.Slice(offset, 4));
            if (messageLength < 16 || offset + messageLength > data.Length)
            {
                throw new InvalidDataException("Netlink 地址消息长度无效。");
            }

            var message = data.Slice(offset, messageLength);
            var type = BinaryPrimitives.ReadUInt16LittleEndian(message.Slice(4, 2));
            offset += (messageLength + 3) & ~3;
            if (type == NlMsgDone)
            {
                completed = true;
                break;
            }

            if (type == NlMsgError)
            {
                var error = BinaryPrimitives.ReadInt32LittleEndian(message.Slice(16, 4));
                if (error != 0)
                {
                    throw new IOException($"Netlink 地址查询失败，错误码 {error}。");
                }

                continue;
            }

            if (type != RtmNewAddress || message.Length < 24 || message[16] != (byte)AddressFamily.InterNetworkV6)
            {
                continue;
            }

            if (TryParseAddress(message, out var key, out var metadata))
            {
                result[key] = metadata;
            }
        }

        return result;
    }

    private static bool TryParseAddress(
        ReadOnlySpan<byte> message,
        out LocalAddressKey key,
        out LocalAddressMetadata metadata)
    {
        key = null!;
        metadata = null!;
        var flags = (uint)message[18];
        IPAddress? address = null;
        string? interfaceName = null;
        uint preferredLifetime = uint.MaxValue;
        uint validLifetime = uint.MaxValue;
        for (var offset = 24; offset + 4 <= message.Length;)
        {
            var length = BinaryPrimitives.ReadUInt16LittleEndian(message.Slice(offset, 2));
            if (length < 4 || offset + length > message.Length)
            {
                throw new InvalidDataException("Netlink 地址属性长度无效。");
            }

            var type = BinaryPrimitives.ReadUInt16LittleEndian(message.Slice(offset + 2, 2));
            var value = message.Slice(offset + 4, length - 4);
            switch (type)
            {
                case IfaAddress when value.Length == 16:
                    address = new IPAddress(value);
                    break;
                case IfaLabel:
                    interfaceName = Encoding.UTF8.GetString(value).TrimEnd('\0');
                    break;
                case IfaCacheInfo when value.Length >= 16:
                    preferredLifetime = BinaryPrimitives.ReadUInt32LittleEndian(value);
                    validLifetime = BinaryPrimitives.ReadUInt32LittleEndian(value.Slice(4, 4));
                    break;
                case IfaFlags when value.Length >= 4:
                    flags |= BinaryPrimitives.ReadUInt32LittleEndian(value);
                    break;
            }

            offset += (length + 3) & ~3;
        }

        if (address is null || string.IsNullOrWhiteSpace(interfaceName))
        {
            return false;
        }

        var temporary = (flags & IfaFTemporary) != 0;
        var deprecated = (flags & IfaFDeprecated) != 0 ||
                         (preferredLifetime == 0 && validLifetime != 0 && validLifetime != uint.MaxValue);
        key = new LocalAddressKey(interfaceName, address);
        metadata = new LocalAddressMetadata
        {
            IsTemporary = temporary,
            IsDeprecated = deprecated,
            SuffixOrigin = temporary ? SuffixOrigin.Random : null
        };
        return true;
    }

    private static int OpenSocket()
    {
        var descriptor = SocketNative(16, 3, NetlinkRoute);
        if (descriptor < 0)
        {
            throw LastError("创建 Netlink 套接字失败");
        }

        return descriptor;
    }

    private static IOException LastError(string operation)
    {
        return new IOException($"{operation}，系统错误码 {Marshal.GetLastPInvokeError()}。");
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SockAddrNl
    {
        public ushort Family;
        public ushort Padding;
        public uint ProcessId;
        public uint Groups;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PollFd
    {
        public int Descriptor;
        public short Events;
        public short ReturnedEvents;
    }

    [DllImport("libc", EntryPoint = "socket", SetLastError = true)]
    private static extern int SocketNative(int domain, int type, int protocol);

    [DllImport("libc", EntryPoint = "bind", SetLastError = true)]
    private static extern int Bind(int descriptor, ref SockAddrNl address, uint addressLength);

    [DllImport("libc", EntryPoint = "sendto", SetLastError = true)]
    private static extern nint SendTo(
        int descriptor,
        byte[] buffer,
        nuint length,
        int flags,
        ref SockAddrNl address,
        uint addressLength);

    [DllImport("libc", EntryPoint = "poll", SetLastError = true)]
    private static extern int Poll(PollFd[] descriptors, nuint count, int timeoutMilliseconds);

    [DllImport("libc", EntryPoint = "recv", SetLastError = true)]
    private static extern nint Receive(int descriptor, byte[] buffer, nuint length, int flags);

    [DllImport("libc", EntryPoint = "close")]
    private static extern int CloseSocket(int descriptor);
}
