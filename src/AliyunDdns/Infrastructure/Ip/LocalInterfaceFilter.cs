namespace AliyunDdns.Infrastructure.Ip;

public static class LocalInterfaceFilter
{
    public static bool IsCommonContainerBridge(string interfaceName)
    {
        return interfaceName.StartsWith("docker", StringComparison.OrdinalIgnoreCase) ||
               interfaceName.StartsWith("br-", StringComparison.OrdinalIgnoreCase) ||
               interfaceName.StartsWith("veth", StringComparison.OrdinalIgnoreCase) ||
               interfaceName.StartsWith("vbr", StringComparison.OrdinalIgnoreCase) ||
               interfaceName.StartsWith("virbr", StringComparison.OrdinalIgnoreCase);
    }
}
