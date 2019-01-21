using Aliyun.Acs.Alidns.Model.V20150109;
using Aliyun.Acs.Core;
using Aliyun.Acs.Core.Profile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using static Aliyun.Acs.Alidns.Model.V20150109.DescribeSubDomainRecordsResponse;

namespace aliyun_ddns
{
    /// <summary>
    /// 域名更新类。
    /// </summary>
    public class DomainUpdater
    {
        /// <summary>
        /// 运行配置。
        /// </summary>
        private readonly Options _op;
        /// <summary>
        /// 每次请求的记录上限。
        /// </summary>
        private const long _pageSize = 100;

        /// <summary>
        /// IP和记录类型。
        /// </summary>
        private enum IpType
        {
            /// <summary>
            /// A记录，IPv4。
            /// </summary>
            A,
            /// <summary>
            /// AAAA记录，IPv6。
            /// </summary>
            AAAA,
            /// <summary>
            /// 任意记录类型。
            /// </summary>
            ANY
        }

        public DomainUpdater(Options op)
        {
            _op = op;
        }

        /// <summary>
        /// 运行自动更新。
        /// </summary>
        public void Run()
        {
            TimeSpan maxWait = new TimeSpan(0, 0, _op.REDO);
            string[] domains = _op.DOMAIN.Split(',', StringSplitOptions.RemoveEmptyEntries);
            HashSet<IpType> targetTypes = GetTargetTypes();
            if (targetTypes.Count == 0)
            {
                Log.Print("没有设置需要修改的记录类型。");
                return;
            }

            while (true)
            {
                DateTime start = DateTime.Now;
                HashSet<IpType> types = new HashSet<IpType>(GetSupportIpTypes());
                types.IntersectWith(targetTypes);
                Dictionary<IpType, string> ips = new Dictionary<IpType, string>();
                foreach (var i in types)
                {
                    string ip = GetIp(i);
                    if (string.IsNullOrWhiteSpace(ip) == false)
                    {
                        ips[i] = ip;
                    }
                }

                foreach (var i in domains)
                {
                    var rds = GetRecords(i);//获取域名的所有记录
                    Dictionary<IpType, Record> oldRds = null;
                    if (types.Count == rds.Count)
                    {
                        oldRds = new Dictionary<IpType, Record>();
                        foreach (var j in ips)
                        {
                            var oldRd = GetOldRecord(j.Key, rds);
                            if (oldRd == null)
                            {
                                oldRds = null;
                                break;
                            }
                            else
                            {
                                oldRds[j.Key] = oldRd;
                            }
                        }
                    }

                    if (rds.Count > 0 && oldRds == null)
                    {
                        DeleteRecords(rds[0]);
                    }

                    foreach (var j in ips)
                    {
                        if (oldRds != null && oldRds.ContainsKey(j.Key))
                        {
                            UpdateRecord(oldRds[j.Key], j.Value);
                        }
                        else
                        {
                            AddRecord(j.Key, i, j.Value);
                        }
                    }
                }

                var used = DateTime.Now - start;
                if (used < maxWait)
                {
                    Thread.Sleep(maxWait - used);
                }
            }
        }

        /// <summary>
        /// 获取目标记录类型。
        /// </summary>
        /// <returns>目标记录类型的集合</returns>
        private HashSet<IpType> GetTargetTypes()
        {
            HashSet<string> inputTypes = new HashSet<string>(_op.TYPE.Split(',', StringSplitOptions.RemoveEmptyEntries));
            HashSet<IpType> targetTypes = new HashSet<IpType>();
            if (inputTypes.Contains("A"))
            {
                targetTypes.Add(IpType.A);
            }

            if (inputTypes.Contains("AAAA"))
            {
                targetTypes.Add(IpType.AAAA);
            }

            return targetTypes;
        }

        /// <summary>
        /// 获取当前记录。
        /// </summary>
        /// <param name="type">记录类型</param>
        /// <param name="rds">记录合集</param>
        /// <returns>有且仅有一条同类型记录时，返回该记录，否则返回null。</returns>
        private Record GetOldRecord(IpType type, IEnumerable<Record> rds)
        {
            Record res = null;
            string t = type.ToString();
            foreach (var i in rds)
            {
                if (i.Type == t)
                {
                    if (res != null)
                    {
                        return null;
                    }
                    else
                    {
                        res = i;
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// 获取本机支持的记录类型。
        /// </summary>
        /// <returns>记录类型的集合</returns>
        private IEnumerable<IpType> GetSupportIpTypes()
        {
            try
            {
                HashSet<IpType> res = new HashSet<IpType>();
                //获取说有网卡信息
                NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface adapter in nics)
                {
                    try
                    {
                        if (adapter.NetworkInterfaceType != NetworkInterfaceType.Ethernet &&
                            adapter.NetworkInterfaceType != NetworkInterfaceType.Wireless80211 &&
                            adapter.NetworkInterfaceType != NetworkInterfaceType.Ppp)
                        {
                            continue;
                        }

                        //获取网络接口信息
                        IPInterfaceProperties properties = adapter.GetIPProperties();
                        //获取单播地址集
                        UnicastIPAddressInformationCollection ips = properties.UnicastAddresses;
                        foreach (UnicastIPAddressInformation i in ips)
                        {
                            switch (i.Address.AddressFamily)
                            {
                                case AddressFamily.InterNetwork:
                                    res.Add(IpType.A);
                                    break;

                                case AddressFamily.InterNetworkV6:
                                    if (i.Address.IsIPv6LinkLocal == false &&
                                        i.Address.IsIPv6Multicast == false &&
                                        i.Address.IsIPv6SiteLocal == false)
                                    {
                                        res.Add(IpType.AAAA);
                                    }

                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
                
                return res;
            }
            catch (Exception e)
            {
                Log.Print($"获取网卡信息时出现异常：{ e }");
                return new IpType[] { IpType.A, IpType.AAAA };
            }
        }

        /// <summary>
        /// 获取新的阿里云客户端。
        /// </summary>
        /// <returns>新的阿里云客户端</returns>
        private DefaultAcsClient GetNewClient()
        {
            var clientProfile = DefaultProfile.GetProfile(_op.ENDPOINT, _op.AKID, _op.AKSCT);
            return new DefaultAcsClient(clientProfile);
        }

        /// <summary>
        /// 获取已经存在的记录。
        /// </summary>
        /// <param name="domain">域名或子域名</param>
        /// <returns>记录的集合，获取失败时返回null。</returns>
        private IList<Record> GetRecords(string domain)
        {
            List<Record> records = new List<Record>();
            try
            {
                var client = GetNewClient();
                long pageNumber = 1;
                do
                {
                    DescribeSubDomainRecordsRequest request = new DescribeSubDomainRecordsRequest
                    {
                        SubDomain = domain,
                        PageSize = _pageSize,
                        PageNumber = pageNumber
                    };

                    var response = client.GetAcsResponse(request);
                    records.AddRange(response.DomainRecords);
                    if (response.TotalCount <= records.Count)
                    {
                        break;
                    }
                    else
                    {
                        ++pageNumber;
                    }
                } while (true);
                Log.Print($"成功获取{ domain }的所有记录，共{ records.Count }条。");
            }
            catch (Exception e)
            {
                Log.Print($"获取{ domain }的所有记录时出现异常：{ e }");
                return null;
            }

            return records;
        }

        /// <summary>
        /// 删除所有记录。
        /// </summary>
        /// <param name="rd">待删除的记录。</param>
        /// <returns>删除成功返回true，否则返回false。</returns>
        private bool DeleteRecords(Record rd)
        {
            try
            {
                var client = GetNewClient();
                DeleteSubDomainRecordsRequest request = new DeleteSubDomainRecordsRequest
                {
                    DomainName = rd.DomainName,
                    RR = rd.RR
                };
                var response = client.GetAcsResponse(request);
                if (response.HttpResponse.isSuccess())
                {
                    Log.Print($"成功清理记录{ rd.RR }.{ rd.DomainName }。");
                    return true;
                }
                else
                {
                    Log.Print($"清理记录{ rd.RR }.{ rd.DomainName }失败。");
                    return false;
                }
            }
            catch (Exception e)
            {
                Log.Print($"删除{ rd.RR }.{ rd.DomainName }记录时出现异常：{ e }");
                return false;
            }
        }

        /// <summary>
        /// 添加解析记录。
        /// </summary>
        /// <param name="type">记录类型</param>
        /// <param name="domain">域名或子域名</param>
        /// <param name="ip">公网ip</param>
        /// <returns>添加成功返回true，否则返回false。</returns>
        private bool AddRecord(IpType type, string domain, string ip)
        {
            try
            {
                string pattern = @"^(\S*)\.(\S+)\.(\S+)$";
                Regex regex = new Regex(pattern);
                var match = regex.Match(domain);
                string domainName;
                string rr;
                if (match.Success)
                {
                    rr = match.Groups[1].Value;
                    domainName = match.Groups[2].Value + "." + match.Groups[3].Value;
                }
                else
                {
                    rr = "@";
                    domainName = domain;
                }

                var client = GetNewClient();
                AddDomainRecordRequest request = new AddDomainRecordRequest
                {
                    DomainName = domainName,
                    RR = rr,
                    Type = type.ToString(),
                    Value = ip,
                    TTL = _op.TTL
                };
                var response = client.GetAcsResponse(request);
                if (response.HttpResponse.isSuccess())
                {
                    Log.Print($"成功增加{ type.ToString() }记录{ domain }为{ ip }。");
                    return true;
                }
                else
                {
                    Log.Print($"增加{ type.ToString() }记录{ domain }失败。");
                    return false;
                }
            }
            catch (Exception e)
            {
                Log.Print($"增加{ type.ToString() }记录{ domain }时发生异常： { e }");
                return false;
            }
        }

        /// <summary>
        /// 更新解析记录。
        /// </summary>
        /// <param name="rd">原记录。</param>
        /// <param name="ip">公网IP。</param>
        /// <returns>更新成功返回true，否则返回false。</returns>
        private bool UpdateRecord(Record rd, string ip)
        {
            if (string.IsNullOrEmpty(ip))
            {
                return false;
            }
            else if (ip == rd.Value)
            {
                Log.Print($"{ rd.Type }记录{ rd.RR }.{ rd.DomainName }不需要更新。");
                return true;
            }

            try
            {
                var client = GetNewClient();
                UpdateDomainRecordRequest request = new UpdateDomainRecordRequest
                {
                    RecordId = rd.RecordId,
                    RR = rd.RR,
                    Type = rd.Type,
                    Value = ip,
                    TTL = _op.TTL
                };
                var response = client.GetAcsResponse(request);
                if (response.HttpResponse.isSuccess())
                {
                    Log.Print($"成功更新{ rd.Type }记录{ rd.RR }.{ rd.DomainName }为{ ip }。");
                    return true;
                }
                else
                {
                    Log.Print($"更新{ rd.Type }记录{ rd.RR }.{ rd.DomainName }失败。");
                    return false;
                }
            }
            catch (Exception e)
            {
                Log.Print($"更新{ rd.Type }记录{ rd.RR }.{ rd.DomainName }时出现异常：{ e }。");
                return false;
            }
        }

        /// <summary>
        /// 获取公网IP。
        /// </summary>
        /// <param name="type">IP类型</param>
        /// <returns>IP字符串</returns>
        private string GetIp(IpType type)
        {
            return type == IpType.A ? PublicIpGetter.GetIpv4() : PublicIpGetter.GetIpv6();
        }
    }
}
