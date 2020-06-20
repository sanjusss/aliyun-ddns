using aliyun_ddns.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace aliyun_ddns.IPGetter.IPv6Getter
{
    public static class IPv6GetterCreator
    {
        private static readonly IEnumerable<IIPv6Getter> _definedGetters = new CommonIPv6Getter[]
               {
                    new CommonIPv6Getter("dyndns.com接口", "http://checkipv6.dyndns.com/", 101),
                    new CommonIPv6Getter("ip.sb接口", "https://api-ipv6.ip.sb/ip", 101),
                    new CommonIPv6Getter("ident.me接口", "http://v6.ident.me/", 101),
                    new CommonIPv6Getter("ipify.org接口", "https://api6.ipify.org/", 101),
                    new CommonIPv6Getter("test-ipv6.com接口", "https://ipv6.lookup.test-ipv6.com/ip/", 100)
               };

        public static IEnumerable<IIPv6Getter> Create()
        {
            List<IIPv6Getter> getters = new List<IIPv6Getter>();
            getters.AddRange(InstanceCreator.Create<IIPv6Getter>(Ignore.CheckType));
            getters.AddRange(_definedGetters);
            return getters;
        }
    }
}
