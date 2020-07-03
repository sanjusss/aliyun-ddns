using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace aliyun_ddns.IPGetter.IPv4Getter
{
    /// <summary>
    /// 获取所有网卡信息，返回最后一个IPv6地址。
    /// </summary>
    [Ignore]
    public sealed class LocalIPv4Getter : BaseLocalIPGetter, IIPv4Getter
    {
        public override string Description => "读取网卡IPv4设置";

        public override int Order => 200;

        protected override string IPNetworks => Options.Instance.IPv4Networks;

        protected override bool CheckIPAddressInformation(IPAddressInformation info)
        {
            return info.Address.AddressFamily == AddressFamily.InterNetwork;
        }
    }
}
