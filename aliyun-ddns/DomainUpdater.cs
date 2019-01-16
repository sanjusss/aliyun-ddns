using Aliyun.Acs.Alidns.Model.V20150109;
using Aliyun.Acs.Core;
using Aliyun.Acs.Core.Profile;
using System;
using System.Collections.Generic;
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
        private static readonly Options _op = Options.GetOptionsFromEnvironment();
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
            /// CNAME记录
            /// </summary>
            CNAME
        }

        /// <summary>
        /// 运行自动更新。
        /// </summary>
        public void Run()
        {
            TimeSpan maxWait = new TimeSpan(0, 0, _op.REDO);
            while (true)
            {
                DateTime start = DateTime.Now;
                Process(IpType.A, _op.DOMAIN);
                Process(IpType.AAAA, _op.DOMAIN);
                var used = DateTime.Now - start;
                if (used < maxWait)
                {
                    Thread.Sleep(maxWait - used);
                }
            }
        }

        /// <summary>
        /// 处理域名更新。
        /// </summary>
        /// <param name="type">记录类型</param>
        /// <param name="domain">域名</param>
        /// <returns>是否成功。</returns>
        private bool Process(IpType type, string domain)
        {
            var cnames = GetRecords(IpType.CNAME.ToString(), domain);
            if (cnames != null)
            {
                if (cnames.Count > 0 && DeleteRecords(cnames[0]) == false)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            var rds = GetRecords(type.ToString(), domain);
            if (rds == null)
            {
                return false;
            }

            if (rds.Count == 1)
            {
                return UpdateRecord(rds[0]);
            }
            else
            {
                if (rds.Count > 1)
                {
                    if (DeleteRecords(rds[0]) == false)
                    {
                        return false;
                    }
                }

                return AddRecord(type, domain);
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
        /// <param name="type">记录类型</param>
        /// <param name="domain">域名或子域名</param>
        /// <returns>记录的集合，获取失败时返回null。</returns>
        private IList<Record> GetRecords(string type, string domain)
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
                        PageNumber = pageNumber,
                        Type = type
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
                Log.Print($"成功获取{ domain }的{ type }记录，共{ records.Count }条。");
            }
            catch (Exception e)
            {
                Log.Print($"获取{ domain }的{ type }记录时出现异常：{ e }");
                return null;
            }

            return records;
        }

        /// <summary>
        /// 删除所有同类记录。
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
                    RR = rd.RR,
                    Type = rd.Type
                };
                var response = client.GetAcsResponse(request);
                if (response.HttpResponse.isSuccess())
                {
                    Log.Print($"成功清理{ rd.Type }记录{ rd.RR }.{ rd.DomainName }。");
                    return true;
                }
                else
                {
                    Log.Print($"清理{ rd.Type }记录{ rd.RR }.{ rd.DomainName }失败。");
                    return false;
                }
            }
            catch (Exception e)
            {
                Log.Print($"删除{ rd.RR }.{ rd.DomainName }的{ rd.Type }记录时出现异常：{ e }");
                return false;
            }
        }

        /// <summary>
        /// 添加解析记录。
        /// </summary>
        /// <param name="type">记录类型</param>
        /// <param name="domain">域名或子域名</param>
        /// <returns>添加成功返回true，否则返回false。</returns>
        private bool AddRecord(IpType type, string domain)
        {
            string ip = type == IpType.A ? PublicIpGetter.GetIpv4() : PublicIpGetter.GetIpv6();
            if (string.IsNullOrEmpty(ip))
            {
                return false;
            }

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
        /// <returns>更新成功返回true，否则返回false。</returns>
        private bool UpdateRecord(Record rd)
        {
            string ip = rd.Type == IpType.A.ToString() ? PublicIpGetter.GetIpv4() : PublicIpGetter.GetIpv6();
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
    }
}
