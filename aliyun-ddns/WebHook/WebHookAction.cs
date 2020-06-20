using aliyun_ddns.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;

namespace aliyun_ddns.WebHook
{
    public static class WebHookAction
    {
        public static void Push(IEnumerable<WebHookItem> items)
        {
            string url = Options.Instance.WEBHOOK;
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            try
            {
                DateTime now = DateTime.Now;
                StringBuilder textBuilder = new StringBuilder($"以下域名在{ now }发生变化：");
                foreach (var i in items)
                {
                    textBuilder.Append($"\n{ i.domain }({ i.recordType }) => { i.ip }");
                }

                dynamic o = new
                {
                    timestamp = ToTimestamp(now),
                    changes = items,
                    msgtype = "text",
                    text = new
                    {
                        content = textBuilder.ToString()
                    }
                };

                string content = JsonConvert.SerializeObject(o);
                using (var http = new HttpClient())
                {
                    StringContent sc = new StringContent(content, Encoding.UTF8, "application/json");
                    http.PostAsync(url, sc).Wait();
                }

                Log.Print("成功推送WebHook。");
            }
            catch (Exception e)
            {
                Log.Print($"推送WebHook时发生异常：\n{ e }");
            }
        }

        private static long ToTimestamp(DateTime dt)
        {
            return (dt.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
        }
    }
}
