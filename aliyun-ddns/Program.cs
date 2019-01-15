using Aliyun.Acs.Alidns.Model.V20150109;
using Aliyun.Acs.Core;
using Aliyun.Acs.Core.Profile;
using Newtonsoft.Json;
using System;

namespace aliyun_ddns
{
    class Program
    {
        static void Main(string[] args)
        {
            var clientProfile = DefaultProfile.GetProfile("cn-hangzhou");
            DefaultAcsClient client = new DefaultAcsClient(clientProfile);

            long count = 0;
            long pageNumber = 1;
            do
            {
                DescribeSubDomainRecordsRequest request = new DescribeSubDomainRecordsRequest();
                request.SubDomain = "";
                request.PageSize = 1;
                request.PageNumber = pageNumber;
                var response = client.GetAcsResponse(request);
                foreach (var i in response.DomainRecords)
                {
                    Console.WriteLine($"Type={ i.Type } VALUE={ i.Value }");
                    ++count;
                }

                if (response.TotalCount <= count)
                {
                    break;
                }
                else
                {
                    ++pageNumber;
                }
            } while (true);
            Console.Read();
        }
    }
}
