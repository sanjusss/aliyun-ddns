using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace aliyun_ddns
{
    /// <summary>
    /// 公网IP获取类。
    /// </summary>
    public static class PublicIpGetter
    {
        /// <summary>
        /// 获取公网IPv4。
        /// </summary>
        /// <returns>返回公网IPv4，如果获取失败，返回null。</returns>
        public static string GetIpv4()
        {
            try
            {
                var request = WebRequest.Create("https://ipv4.lookup.test-ipv6.com/ip/");
                var response = request.GetResponse();
                using (StreamReader stream = new StreamReader(response.GetResponseStream()))
                {
                    string json = stream.ReadToEnd();
                    dynamic result = JsonConvert.DeserializeObject(json);
                    string ip = result.ip;
                    if (string.IsNullOrWhiteSpace(ip) == false)
                    {
                        Log.Print($"当前公网IPv4为{ ip }。");
                        return result.ip;
                    }
                    else
                    {
                        Log.Print("没有检测到公网IPv4。");
                        return null;
                    }
                }
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
        public static string GetIpv6()
        {
            try
            {
                var request = WebRequest.Create("https://ipv6.lookup.test-ipv6.com/ip/");
                var response = request.GetResponse();
                using (StreamReader stream = new StreamReader(response.GetResponseStream()))
                {
                    string json = stream.ReadToEnd();
                    dynamic result = JsonConvert.DeserializeObject(json);
                    string ip = result.ip;
                    if (string.IsNullOrWhiteSpace(ip) == false)
                    {
                        Log.Print($"当前公网IPv6为{ ip }。");
                        return result.ip;
                    }
                    else
                    {
                        Log.Print("没有检测到公网IPv6。");
                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Print($"检测公网IPv6时发生异常：{ e.Message }");
                return null;
            }
        }
    }
}
