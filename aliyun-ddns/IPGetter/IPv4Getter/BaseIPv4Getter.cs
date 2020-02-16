using System;
using System.Collections.Generic;
using System.Text;

namespace aliyun_ddns.IPGetter.IPv4Getter
{
    public abstract class BaseIPv4Getter : BaseIPGetter, IIPv4Getter
    {
        protected override string IPPattern => @"(25[0-5]|2[0-4]\d|[0-1]\d{2}|[1-9]?\d)\.(25[0-5]|2[0-4]\d|[0-1]\d{2}|[1-9]?\d)\.(25[0-5]|2[0-4]\d|[0-1]\d{2}|[1-9]?\d)\.(25[0-5]|2[0-4]\d|[0-1]\d{2}|[1-9]?\d)";
    }
}
