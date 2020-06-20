using System;
using System.Collections.Generic;
using System.Text;

namespace aliyun_ddns.IPGetter.IPv6Getter
{
    [Ignore]
    public class CommonIPv6Getter : BaseIPv6Getter
    {
        private readonly string _description;
        public override string Description => _description;

        private readonly string _url;
        protected override string Url => _url;

        private readonly int _order;
        public override int Order => _order;

        public CommonIPv6Getter(string description, string url, int order = 50)
        {
            _description = description;
            _url = url;
            _order = order;
        }
    }
}
