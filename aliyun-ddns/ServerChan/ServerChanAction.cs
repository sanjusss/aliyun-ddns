using aliyun_ddns.Common;
using aliyun_ddns.WebHook;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;

namespace aliyun_ddns.ServerChan
{
    public static class ServerChanAction
    {
        public static void Push(IEnumerable<WebHookItem> items)
        {
            string sendKey = Options.Instance.SendKey;
            if (string.IsNullOrWhiteSpace(sendKey))
            {
                return;
            }

            try
            {
                StringBuilder url = new StringBuilder("https://sctapi.ftqq.com/");
                url.Append(sendKey).Append(".send");
                DateTime now = DateTime.Now;
                StringBuilder textBuilder = new StringBuilder($"以下域名在{ now }发生变化：");
                foreach (var i in items)
                {
                    textBuilder.Append($"\n{ i.domain }({ i.recordType }) => { i.ip }");
                }

                dynamic o = new
                {
                    title = "域名变化通知",
                    desp  = textBuilder.ToString()
                };

                string content = JsonConvert.SerializeObject(o);
                using (var http = new HttpClient())
                {
                    StringContent sc = new StringContent(content, Encoding.UTF8, "application/json");
                    http.PostAsync(url.ToString(), sc).Wait();
                }

                Log.Print("成功推送Server酱。");
            }
            catch (Exception e)
            {
                Log.Print($"推送Server酱时发生异常：\n{ e }");
            }
        }

        private static long ToTimestamp(DateTime dt)
        {
            return (dt.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
        }
    }
}
