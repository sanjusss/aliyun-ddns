using Aliyun.Acs.Alidns.Model.V20150109;
using Aliyun.Acs.Core;
using Aliyun.Acs.Core.Profile;
using aliyun_ddns.Common;
using aliyun_ddns.WebHook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static Aliyun.Acs.Alidns.Model.V20150109.DescribeSubDomainRecordsResponse;

namespace aliyun_ddns
{
    using Record = DescribeSubDomainRecords_Record;
    /// <summary>
    /// 域名更新类。
    /// </summary>
    public class DomainUpdater
    {
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

        /// <summary>
        /// 运行自动更新。
        /// </summary>
        public void Run()
        {
            TimeSpan maxWait = new TimeSpan(0, 0, Options.Instance.Redo);
            string[] domains = Options.Instance.Domain.Split(',', StringSplitOptions.RemoveEmptyEntries);
            HashSet<IpType> targetTypes = GetTargetTypes();
            if (targetTypes.Count == 0)
            {
                Log.Print("没有设置需要修改的记录类型。");
                return;
            }

            while (true)
            {
                DateTime start = DateTime.Now;

                try
                {
                    Update(domains, targetTypes);
                }
                catch (Exception e)
                {
                    Log.Print($"更新时出现未预料的异常：{ e }");
                }

                var used = DateTime.Now - start;
                if (used < maxWait)
                {
                    Thread.Sleep(maxWait - used);
                }
            }
        }

        private void Update(string[] domains, HashSet<IpType> targetTypes)
        {
            HashSet<IpType> types = new HashSet<IpType>(GetSupportIpTypes());
            types.IntersectWith(targetTypes);
            var tasks = types.ToDictionary(t => t, t => t == IpType.A ? PublicIPGetter.GetIpv4() : PublicIPGetter.GetIpv6());
            Task.WaitAll(tasks.Values.ToArray());
            var ips = tasks.ToDictionary(p => p.Key, p => p.Value.Result);

            List<WebHookItem> items = new List<WebHookItem>();
            foreach (var i in domains)
            {
                var oldRecords = GetRecords(i);//获取域名的所有记录
                if (oldRecords == null)
                {
                    Log.Print($"跳过设置域名 { i }");
                    continue;
                }

                ClearCName(ref oldRecords);//清理必然重复的CNAME记录。

                foreach (var j in ips)
                {
                    string type = j.Key.ToString();
                    //获取当前类型的所有记录。
                    List<Record> typedRecords = new List<Record>();
                    for (int k = oldRecords.Count - 1; k >= 0; --k)
                    {
                        if (oldRecords[k].Type == type)
                        {
                            typedRecords.Add(oldRecords[k]);
                            oldRecords.RemoveAt(k);
                        }
                    }

                    //根据已有记录数量决定操作。
                    bool success = false;
                    if (typedRecords.Count == 1)
                    {
                        success = UpdateRecord(typedRecords[0], j.Value);
                    }
                    else
                    {
                        if (typedRecords.Count > 1)
                        {
                            DeleteRecords(typedRecords[0].DomainName, typedRecords[0].RR, type);
                        }

                        success = AddRecord(j.Key, i, j.Value);
                    }

                    if (success)
                    {
                        items.Add(new WebHookItem
                        {
                            recordType = j.Key.ToString(),
                            domain = i,
                            ip = j.Value
                        });
                    }
                }
            }

            if (items.Count > 0)
            {
                WebHookAction.Push(items);
            }
        }

        /// <summary>
        /// 获取目标记录类型。
        /// </summary>
        /// <returns>目标记录类型的集合</returns>
        private HashSet<IpType> GetTargetTypes()
        {
            HashSet<string> inputTypes = new HashSet<string>(Options.Instance.Type.Split(',', StringSplitOptions.RemoveEmptyEntries));
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
            //var clientProfile = DefaultProfile.GetProfile(Options.Instance.ENDPOINT, Options.Instance.AKID, Options.Instance.AKSCT);
            //return new DefaultAcsClient(clientProfile);
            return new DefaultAcsClient(DefaultProfile.GetProfile(),
                new Aliyun.Acs.Core.Auth.BasicCredentials(Options.Instance.Akid, Options.Instance.Aksct));
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
        /// 删除所有指定类型的记录。
        /// </summary>
        /// <param name="domain">域名</param>
        /// <param name="rr">主机记录</param>
        /// <param name="type">解析记录类型</param>
        /// <returns>是否删除成功。</returns>
        private bool DeleteRecords(string domain, string rr, string type)
        {
            try
            {
                var client = GetNewClient();
                DeleteSubDomainRecordsRequest request = new DeleteSubDomainRecordsRequest
                {
                    DomainName = domain,
                    RR = rr,
                    Type = type
                };
                var response = client.GetAcsResponse(request);
                if (response.HttpResponse.isSuccess())
                {
                    Log.Print($"成功清理{ type }记录{ rr }.{ domain }。");
                    return true;
                }
                else
                {
                    Log.Print($"清理{ type }记录{ rr }.{ domain }失败。");
                    return false;
                }
            }
            catch (Exception e)
            {
                Log.Print($"删除{ type }记录{ rr }.{ domain }记录时出现异常：{ e }");
                return false;
            }
        }

        /// <summary>
        /// 删除多余的CNAME记录。
        /// </summary>
        /// <param name="rds">记录集合</param>
        /// <returns>是否完全删除成功。</returns>
        private bool ClearCName(ref IList<Record> rds)
        {
            bool found = false;
            string domain = null;
            string rr = null;
            for (int i = rds.Count - 1; i >= 0; --i)
            {
                if (rds[i].Type == "CNAME")
                {
                    if (found == false)
                    {
                        found = true;
                        domain = rds[i].DomainName;
                        rr = rds[i].RR;
                    }

                    rds.RemoveAt(i);
                }
            }

            return found == false || DeleteRecords(domain, rr, "CNAME");
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
                    _Value = ip,
                    TTL = Options.Instance.TTL
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
            else if (ip == rd._Value)
            {
                Log.Print($"{ rd.Type }记录{ rd.RR }.{ rd.DomainName }不需要更新。");
                return false;
            }

            try
            {
                var client = GetNewClient();
                UpdateDomainRecordRequest request = new UpdateDomainRecordRequest
                {
                    RecordId = rd.RecordId,
                    RR = rd.RR,
                    Type = rd.Type,
                    _Value = ip,
                    TTL = Options.Instance.TTL
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
    }
}
