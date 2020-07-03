using aliyun_ddns.Common;
using aliyun_ddns.IPGetter.IPv4Getter;
using aliyun_ddns.IPGetter.IPv6Getter;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace aliyun_ddns
{
    /// <summary>
    /// 公网IP获取类。
    /// </summary>
    public static class PublicIPGetter
    {
        /// <summary>
        /// 获取公网IPv4。
        /// </summary>
        /// <returns>返回公网IPv4，如果获取失败，返回null。</returns>
        public static async Task<string> GetIpv4()
        {
            try
            {
                var dics = IPv4GetterCreator.Create()
                    .Where(g => Options.Instance.CNIPv4 == false || g.Order < 100)
                    .ToDictionary(g => g.GetIP(), g => g.Description);
                string result = await dics.Keys.WhenAny(task =>
                {
                    string ip = task.Result;
                    if (ip == null)
                    {
                        Log.Print($"从 { dics[task] } 获取公网IPv4失败。");
                    }
                    else
                    {
                        Log.Print($"当前公网IPv4为 { ip }（{ dics[task] }）。");
                    }

                    return ip != null;
                },
                TimeSpan.FromSeconds(30));

                if (result == null)
                {
                    Log.Print($"获取公网IPv4失败，所有API接口均无法返回IPv4地址。");
                }

                return result;
            }
            catch (Exception e)
            {
                Log.Print($"检测公网IPv4时发生异常：{ e.Message }");
                return null;
            }
        }

        /// <summary>
        /// 获取公网IPv6。
        /// </summary>
        /// <returns>返回公网IPv6，如果获取失败，返回null。</returns>
        public static async Task<string> GetIpv6()
        {
            try
            {
                var dics = IPv6GetterCreator.Create().ToDictionary(g => g.GetIP(), g => g.Description);
                string result = await dics.Keys.WhenAny(task =>
                {
                    string ip = task.Result;
                    if (ip == null)
                    {
                        Log.Print($"从 { dics[task] } 获取公网IPv6失败。");
                    }
                    else
                    {
                        Log.Print($"当前公网IPv6为 { ip }（{ dics[task] }）。");
                    }

                    return ip != null;
                },
                TimeSpan.FromSeconds(30));

                if (result == null)
                {
                    Log.Print($"获取公网IPv6失败，所有API接口均无法返回IPv6地址。");
                }

                return result;
            }
            catch (Exception e)
            {
                Log.Print($"检测公网IPv6时发生异常：{ e.Message }");
                return null;
            }
        }
    }
}
