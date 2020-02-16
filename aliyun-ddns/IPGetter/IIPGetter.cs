using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace aliyun_ddns.IPGetter
{
    public interface IIPGetter
    {
        /// <summary>
        /// IP获取器的描述字符串。
        /// </summary>
        public string Description { get; }
        /// <summary>
        /// 优先级。
        /// 仅在顺序模式下有效，越小执行的时间越早。
        /// </summary>
        public int Order { get; }
        /// <summary>
        /// 获取IP。
        /// </summary>
        /// <returns>IP字符串，获取失败时返回null。</returns>
        public Task<string> GetIP();
    }
}
