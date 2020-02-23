using System;
using System.Collections.Generic;
using System.Text;

namespace aliyun_ddns.Common
{
    /// <summary>
    /// 日志输出类。
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// 打印日志到屏幕。
        /// </summary>
        /// <param name="msg">日志信息</param>
        public static void Print(string msg)
        {
            Console.WriteLine($"[{ DateTime.UtcNow.AddHours(Options.Instance.TIMEZONE) }]{ msg }");
        }
    }
}
