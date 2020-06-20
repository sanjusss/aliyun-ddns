using System;
using System.Collections.Generic;
using System.Text;

namespace aliyun_ddns.WebHook
{
    public struct WebHookItem
    {
        public string recordType;
        public string domain;
        public string ip;
    }
}
