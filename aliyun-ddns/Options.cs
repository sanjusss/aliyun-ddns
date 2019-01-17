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
        [Option('e', "endpoint", Required = false, Default = "cn-hangzhou", HelpText = "Region Id，详见https://help.aliyun.com/document_detail/40654.html?spm=a2c4e.11153987.0.0.6d85366aUfTWbG")]
        public string ENDPOINT { get; set; } = "cn-hangzhou";
        [Option('d', "domain", Required = false, Default = "my.domain.com", HelpText = "需要更新的域名，可以用“,”隔开。")]
        public string DOMAIN { get; set; } = "my.domain.com";
        [Option('i', "interval", Required = false, Default = 300, HelpText = "执行域名更新的间隔，单位秒。")]
        public int REDO { get; set; } = 300;
        [Option('t', "ttl", Required = false, Default = 600, HelpText = "服务器缓存解析记录的时长，单位秒。")]
        public long TTL { get; set; } = 600;

        public static Options GetOptionsFromEnvironment(ref Options op)
        {
            op.AKID = Environment.GetEnvironmentVariable("AKID") ?? op.AKID;
            op.AKSCT = Environment.GetEnvironmentVariable("AKSCT") ?? op.AKSCT;
            op.ENDPOINT = Environment.GetEnvironmentVariable("ENDPOINT") ?? op.ENDPOINT;
            op.DOMAIN = Environment.GetEnvironmentVariable("DOMAIN") ?? op.DOMAIN;

            string redoText = Environment.GetEnvironmentVariable("REDO");
            if (int.TryParse(redoText, out int redo))
            {
                op.REDO = redo;
            }

            string ttlText = Environment.GetEnvironmentVariable("TTL");
            if (long.TryParse(ttlText, out long ttl))
            {
                op.TTL = ttl;
            }
            
            return op;
        }
    }
}
