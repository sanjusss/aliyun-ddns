using AliyunDdns.Application;
using AliyunDdns.Configuration;
using AliyunDdns.Infrastructure.Aliyun;
using AliyunDdns.Infrastructure.Ip;
using AliyunDdns.Infrastructure.Notifications;
using AliyunDdns.Infrastructure.Resilience;
using Microsoft.Extensions.Options;

namespace AliyunDdns.Hosting;

public static class ServiceRegistration
{
    public static IServiceCollection AddAliyunDdnsServices(
        this IServiceCollection services,
        DdnsOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton(Options.Create(options));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<DdnsUpdatePlanner>();
        services.AddSingleton(_ => new IpSourceCatalog());
        services.AddSingleton<INetworkInterfaceInventory, NetworkInterfaceInventory>();
        services.AddSingleton<ILocalAddressMetadataSource, LinuxNetlinkAddressMetadataSource>();
        services.AddSingleton<ILocalAddressSource, NetworkInterfaceLocalAddressSource>();
        services.AddSingleton<IIpAddressProvider, PublicIpAddressProvider>();
        services.AddSingleton<IIpAddressProvider, LocalIpAddressProvider>();
        services.AddSingleton<RequestRetryPolicy>();
        services.AddSingleton<IDnsClient, AliyunDnsClient>();
        services.AddSingleton<AliyunZoneResolver>();
        services.AddSingleton<INotificationSink, WebhookNotificationSink>();
        services.AddSingleton<DdnsUpdateService>();
        services.AddHttpClient("public-ip", client => client.Timeout = Timeout.InfiniteTimeSpan);
        services.AddHttpClient("webhook", client => client.Timeout = Timeout.InfiniteTimeSpan);
        return services;
    }
}
