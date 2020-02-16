using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace aliyun_ddns.IPGetter
{
    public abstract class BaseIPGetter : IIPGetter
    {
        /// <summary>
        /// IP获取器的描述字符串。
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// 优先级。
        /// 仅在顺序模式下有效，越小执行的时间越早。
        /// </summary>
        public virtual int Order => 50;

        /// <summary>
        /// 获取公网IP的API地址。
        /// </summary>
        protected abstract string Url { get; }

        /// <summary>
        /// 匹配IP的正则表达式。
        /// </summary>
        protected abstract string IPPattern { get; }

        /// <summary>
        /// 异步的方式获取IP。
        /// 使用Get方法访问Url，将返回值与IPPattern匹配。
        /// </summary>
        /// <returns>返回IP，出错时返回null。</returns>
        public async Task<string> GetIP()
        {
            try
            {
                using HttpClient http = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(30)
                };
                string content = await http.GetStringAsync(Url);
                var match = Regex.Match(content, IPPattern);
                if (match.Success)
                {
                    return match.Value;
                }
                else
                {
                    return null;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
