using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

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
            string res = GetIpv4FromIpIp();
            if (string.IsNullOrEmpty(res) == false)
            {
                return res;
            }

            res = GetIpv4FromTaoBao();
            if (string.IsNullOrEmpty(res) == false)
            {
                return res;
            }

            res = GetIpv4FromIpApi();
            if (string.IsNullOrEmpty(res) == false)
            {
                return res;
            }

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
                        Log.Print($"当前公网IPv4为{ ip }（test-ipv6接口）。");
                        return ip;
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

        private static string GetIpv4FromTaoBao()
        {
            try
            {
                var request = WebRequest.Create("http://ip.taobao.com/service/getIpInfo.php?ip=myip");
                var response = request.GetResponse();
                using (StreamReader stream = new StreamReader(response.GetResponseStream()))
                {
                    string json = stream.ReadToEnd();
                    dynamic result = JsonConvert.DeserializeObject(json);
                    string ip = result.data.ip;
                    if (string.IsNullOrWhiteSpace(ip) == false)
                    {
                        Log.Print($"当前公网IPv4为{ ip }（淘宝接口）。");
                        return ip;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private static string GetIpv4FromIpApi()
        {
            try
            {
                var request = WebRequest.Create("http://ip-api.com/json/?fields=query");
                var response = request.GetResponse();
                using (StreamReader stream = new StreamReader(response.GetResponseStream()))
                {
                    string json = stream.ReadToEnd();
                    dynamic result = JsonConvert.DeserializeObject(json);
                    string ip = result.query;
                    if (string.IsNullOrWhiteSpace(ip) == false)
                    {
                        Log.Print($"当前公网IPv4为{ ip }（ip-api）。");
                        return ip;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private static string GetIpv4FromIpIp()
        {
            try
            {
                var request = WebRequest.Create("http://myip.ipip.net/");
                var response = request.GetResponse();
                using (StreamReader stream = new StreamReader(response.GetResponseStream()))
                {
                    string content = stream.ReadToEnd();
                    var match = Regex.Match(content, @"(25[0-5]|2[0-4]\d|[0-1]\d{2}|[1-9]?\d)\.(25[0-5]|2[0-4]\d|[0-1]\d{2}|[1-9]?\d)\.(25[0-5]|2[0-4]\d|[0-1]\d{2}|[1-9]?\d)\.(25[0-5]|2[0-4]\d|[0-1]\d{2}|[1-9]?\d)");
                    if (match.Success)
                    {
                        string ip = match.Value;
                        Log.Print($"当前公网IPv4为{ ip }（ipip.net接口）。");
                        return ip;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch
            {
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
                        Log.Print($"当前公网IPv6为{ ip }（test-ipv6接口）。");
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
