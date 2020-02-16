using System;
using System.Collections.Generic;
using System.Text;

namespace aliyun_ddns.IPGetter.IPv4Getter
{
    [Ignore]
    public sealed class CommonIPv4Getter : BaseIPv4Getter
    {
        private readonly string _description;
        public override string Description => _description;

        private readonly string _url;
        protected override string Url => _url;

        private readonly int _order;
        public override int Order => _order;

        public CommonIPv4Getter(string description, string url, int order = 50)
        {
            _description = description;
            _url = url;
            _order = order;
        }
    }
}
