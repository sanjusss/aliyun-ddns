using System;

namespace aliyun_ddns
{
    public class Options
    {
        public string AKID { get; set; } = "access key id ";
        public string AKSCT { get; set; } = "access key secret";
        public string ENDPOINT { get; set; } = "cn-hangzhou";
        public string DOMAIN { get; set; } = "my.domain.com";
        public int REDO { get; set; } = 30;
        public long TTL { get; set; } = 60;

        public static Options GetOptionsFromEnvironment()
        {
            Options op = new Options();
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
