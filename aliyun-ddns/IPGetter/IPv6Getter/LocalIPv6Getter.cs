using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace aliyun_ddns.IPGetter.IPv6Getter
{
    /// <summary>
    /// 获取所有网卡信息，返回最后一个IPv6地址。
    /// </summary>
    public sealed class LocalIPv6Getter : IIPv6Getter
    {
        public string Description => "读取网卡设置";

        public int Order => 200;

        public async Task<string> GetIP()
        {
            return await Task.Run(() =>
            {
                Thread.Sleep(3000);
                try
                {
                    string ip = null;
                    //获取所有网卡信息，返回最后一个IPv6地址。
                    NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
                    foreach (NetworkInterface adapter in nics)
                    {
                        try
                        {
                            if (adapter.NetworkInterfaceType != NetworkInterfaceType.Ethernet &&
                                adapter.NetworkInterfaceType != NetworkInterfaceType.Wireless80211 &&
                                adapter.NetworkInterfaceType != NetworkInterfaceType.Ppp)
                            {
                                continue;
                            }

                            //获取网络接口信息
                            IPInterfaceProperties properties = adapter.GetIPProperties();
                            //获取单播地址集
                            UnicastIPAddressInformationCollection ips = properties.UnicastAddresses;
                            foreach (UnicastIPAddressInformation i in ips)
                            {
                                if (i.Address.AddressFamily != AddressFamily.InterNetworkV6 ||
                                    i.Address.IsIPv6LinkLocal ||
                                    i.Address.IsIPv6Multicast ||
                                    i.Address.IsIPv6SiteLocal ||
                                    i.Address.IsIPv4MappedToIPv6 ||
                                    i.Address.IsIPv6Teredo)
                                {
                                    continue;
                                }

                                ip = i.Address.ToString();
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    return ip;
                }
                catch
                {
                    return null;
                }
            });
        }
    }
}
