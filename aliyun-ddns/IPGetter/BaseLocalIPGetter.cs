using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace aliyun_ddns.IPGetter
{
    public abstract class BaseLocalIPGetter : IIPGetter
    {
        public abstract string Description { get; }
        public abstract int Order { get; }

        /// <summary>
        /// IP网段字符串。
        /// </summary>
        protected abstract string IPNetworks { get; }

        /// <summary>
        /// 检查IP信息是否符合要求。
        /// </summary>
        /// <param name="info">IP信息</param>
        /// <returns>是否符合要求</returns>
        protected abstract bool CheckIPAddressInformation(IPAddressInformation info);

        public async Task<string> GetIP()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var nets = GetIPNetworks();
                    string ip = null;
                    //获取所有网卡信息，返回最后一个地址。
                    NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
                    foreach (NetworkInterface adapter in nics)
                    {
                        try
                        {
                            //获取网络接口信息
                            IPInterfaceProperties properties = adapter.GetIPProperties();
                            //获取单播地址集
                            UnicastIPAddressInformationCollection ips = properties.UnicastAddresses;
                            foreach (UnicastIPAddressInformation i in ips)
                            {
                                if (CheckIPAddressInformation(i) == false)
                                {
                                    continue;
                                }

                                bool success = false;
                                foreach (var net in nets)
                                {
                                    if (net.Contains(i.Address))
                                    {
                                        success = true;
                                        break;
                                    }
                                }

                                if (success)
                                {
                                    ip = i.Address.ToString();
                                }
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

        private IEnumerable<IPNetwork> GetIPNetworks()
        {
            List<IPNetwork> nets = new List<IPNetwork>();
            string text = IPNetworks;
            if (string.IsNullOrEmpty(text) == false)
            {
                string[] subs = text.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var i in subs)
                {
                    if (IPNetwork.TryParse(i, out IPNetwork net))
                    {
                        nets.Add(net);
                    }
                }
            }

            return nets;
        }
    }
}
