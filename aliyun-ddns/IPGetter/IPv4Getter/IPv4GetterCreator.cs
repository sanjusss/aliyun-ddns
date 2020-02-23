using aliyun_ddns.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace aliyun_ddns.IPGetter.IPv4Getter
{
    public static class IPv4GetterCreator
    {
        private static readonly IEnumerable<IIPv4Getter> _definedGetters = new CommonIPv4Getter[]
            {
                new CommonIPv4Getter("test-ipv6.com接口", "https://ipv4.lookup.test-ipv6.com/ip/", 100),
                new CommonIPv4Getter("ipip.net接口", "http://myip.ipip.net/"),
                new CommonIPv4Getter("ip-api.com接口", "http://ip-api.com/json/?fields=query", 100),
                new CommonIPv4Getter("淘宝接口", "http://ip.taobao.com/service/getIpInfo.php?ip=myip"),
                new CommonIPv4Getter("3322接口", "http://ip.3322.net/"),
            };

        public static IEnumerable<IIPv4Getter> Create()
        {
            List<IIPv4Getter> getters = new List<IIPv4Getter>();
            getters.AddRange(InstanceCreator.Create<IIPv4Getter>(Ignore.CheckType));
            getters.AddRange(_definedGetters);
            return getters;
        }
    }
}
