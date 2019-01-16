
[![Build status](https://ci.appveyor.com/api/projects/status/r585ycrd9wn0v5ed?svg=true)](https://ci.appveyor.com/project/sanjusss/aliyun-ddns)
[![](https://img.shields.io/docker/stars/sanjusss/aliyun-ddns.svg?logo=docker)](https://hub.docker.com/r/sanjusss/aliyun-ddns)
[![GitHub license](https://img.shields.io/github/license/sanjusss/aliyun-ddns.svg)](https://github.com/sanjusss/aliyun-ddns/blob/master/LICENSE)  
[![GitHub tag](https://img.shields.io/github/tag/sanjusss/aliyun-ddns.svg)](https://github.com/sanjusss/aliyun-ddns/tags)
[![GitHub issues](https://img.shields.io/github/issues/sanjusss/aliyun-ddns.svg)](https://github.com/sanjusss/aliyun-ddns/issues)
[![GitHub forks](https://img.shields.io/github/forks/sanjusss/aliyun-ddns.svg)](https://github.com/sanjusss/aliyun-ddns/network)
[![GitHub stars](https://img.shields.io/github/stars/sanjusss/aliyun-ddns.svg)](https://github.com/sanjusss/aliyun-ddns/stargazers)


# 使用方法

```
docker run -d --restart=always \
    -e "AKID=[ALIYUN's AccessKey-ID]" \
    -e "AKSCT=[ALIYUN's AccessKey-Secret]" \
    -e "DOMAIN=ddns.aliyun.win" \
    -e "REDO=30" \
    -e "TTL=60" \
    sanjusss/aliyun-ddns
```
AKID：阿里云的Access Key ID。  
AKSCT：阿里云的Access Key Secret。  
DOMAIN：需要更新的域名。  
REDO： 更新间隔，单位秒。建议大于等于TTL/2。  
TTL： 服务器缓存解析记录的时长，单位秒，普通用户最小为600。  
