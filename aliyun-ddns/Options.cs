using CommandLine;
using System;

namespace aliyun_ddns
{
    public class Options
    {
        [Option('u', "access-key-id", Required = false, Default = "access key id", HelpText = "access key id")]
        public string AKID { get; set; } = "access key id";
        [Option('p', "access-key-secret", Required = false, Default = "access key secret", HelpText = "access key secret")]
        public string AKSCT { get; set; } = "access key secret";
        //[Option('e', "endpoint", Required = false, Default = "cn-hangzhou", HelpText = "Region Id，详见https://help.aliyun.com/document_detail/40654.html?spm=a2c4e.11153987.0.0.6d85366aUfTWbG")]
        //public string ENDPOINT { get; set; } = "cn-hangzhou";
        [Option('d', "domain", Required = false, Default = "my.domain.com", HelpText = "需要更新的域名，可以用“,”隔开。")]
        public string DOMAIN { get; set; } = "my.domain.com";
        [Option('i', "interval", Required = false, Default = 300, HelpText = "执行域名更新的间隔，单位秒。")]
        public int REDO { get; set; } = 300;
        [Option('t', "ttl", Required = false, Default = 600, HelpText = "服务器缓存解析记录的时长，单位秒。")]
        public long TTL { get; set; } = 600;
        [Option("timezone", Required = false, Default = 8.0, HelpText = "输出日志时的时区，单位小时。")]
        public double TIMEZONE { get; set; } = 8;
        [Option("type", Required = false, Default = "A,AAAA", HelpText = "需要更改的记录类型，可以用“,”隔开，只能是“A”、“AAAA”或“A,AAAA”。")]
        public string TYPE { get; set; } = "A,AAAA";
        [Option("cnipv4", Required = false, Default = false, HelpText = "仅使用中国服务器检测公网IPv4地址。")]
        public bool CNIPV4 { get; set; } = false;
        [Option('h',"webhook", Required = false, Default = null, HelpText = "WEB HOOK推送地址。")]
        public string WEBHOOK { get; set; } = null;

        private static Options _instance = null;
        private static object _instanceLocker = new object();
        public static Options Instance 
        {
            get
            {
                lock (_instanceLocker)
                {
                    if (_instance == null)
                    {
                        _instance = new Options();
                        _instance.InitOptionsFromEnvironment();
                    }

                    return _instance;
                }
            }
            set
            {
                lock (_instanceLocker)
                {
                    value.InitOptionsFromEnvironment();
                    _instance = value;
                }
            }
        }

        private void InitOptionsFromEnvironment()
        {
            AKID = GetEnvironmentVariable("AKID") ?? AKID;
            AKSCT = GetEnvironmentVariable("AKSCT") ?? AKSCT;
            //ENDPOINT = GetEnvironmentVariable("ENDPOINT") ?? ENDPOINT;
            DOMAIN = GetEnvironmentVariable("DOMAIN") ?? DOMAIN;
            TYPE = GetEnvironmentVariable("TYPE") ?? TYPE;
            WEBHOOK = GetEnvironmentVariable("WEBHOOK") ?? WEBHOOK;

            string redoText = GetEnvironmentVariable("REDO");
            if (int.TryParse(redoText, out int redo))
            {
                REDO = redo;
            }

            string ttlText = GetEnvironmentVariable("TTL");
            if (long.TryParse(ttlText, out long ttl))
            {
                TTL = ttl;
            }

            string tzText = GetEnvironmentVariable("TIMEZONE");
            if (double.TryParse(tzText, out double tz))
            {
                TIMEZONE = tz;
            }

            string cnipv4Text = GetEnvironmentVariable("CNIPV4");
            if (bool.TryParse(cnipv4Text, out bool cnipv4))
            {
                CNIPV4 = cnipv4;
            }
        }

        private static  string GetEnvironmentVariable(string variable)
        {
            string res = Environment.GetEnvironmentVariable(variable);
            if (string.IsNullOrEmpty(res))
            {
                return null;
            }
            else 
            {
                return res;
            }
        }
    }
}
