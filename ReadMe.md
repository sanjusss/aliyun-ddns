﻿
![GitHub Workflow Status (branch)](https://img.shields.io/github/workflow/status/sanjusss/aliyun-ddns/.NET%20Core)
[![](https://img.shields.io/docker/stars/sanjusss/aliyun-ddns.svg?logo=docker)](https://hub.docker.com/r/sanjusss/aliyun-ddns)
[![GitHub license](https://img.shields.io/github/license/sanjusss/aliyun-ddns.svg)](https://github.com/sanjusss/aliyun-ddns/blob/master/LICENSE)  
[![GitHub tag](https://img.shields.io/github/tag/sanjusss/aliyun-ddns.svg)](https://github.com/sanjusss/aliyun-ddns/tags)
[![GitHub issues](https://img.shields.io/github/issues/sanjusss/aliyun-ddns.svg)](https://github.com/sanjusss/aliyun-ddns/issues)
[![GitHub forks](https://img.shields.io/github/forks/sanjusss/aliyun-ddns.svg)](https://github.com/sanjusss/aliyun-ddns/network)
[![GitHub stars](https://img.shields.io/github/stars/sanjusss/aliyun-ddns.svg)](https://github.com/sanjusss/aliyun-ddns/stargazers)

# 功能
- 通过在线API获取公网IPv4地址，更新到域名A记录。
- 通过在线API获取公网IPv6地址，更新到域名AAAA记录。
- 通过本地网卡获取公网或内网IPv4地址，更新到域名A记录。
- 通过本地网卡获取公网或内网IPv6地址，更新到域名AAAA记录。
- 支持更新多个域名的记录。
- 支持Docker容器，支持x64、ARMv7和ARMv8。
- IP发生变化时，使用WebHook通知。

# 使用方法

### Docker
```
docker run -d --restart=always --net=host \
    -e "AKID=[ALIYUN's AccessKey-ID]" \
    -e "AKSCT=[ALIYUN's AccessKey-Secret]" \
    -e "DOMAIN=ddns.aliyun.win" \
    -e "REDO=30" \
    -e "TTL=60" \
    -e "TIMEZONE=8.0" \
    -e "TYPE=A,AAAA" \
    sanjusss/aliyun-ddns
```
| 环境变量名称 | 注释 | 默认值 |
| :---- | :----- | :--- |
|AKID|阿里云的Access Key ID。[获取阿里云AccessToken](https://usercenter.console.aliyun.com/)|access key id|
|AKSCT|阿里云的Access Key Secret。|access key secret|
|DOMAIN|需要更新的域名，多个域名需要“,”分隔。|my.domain.com|
|REDO|更新间隔，单位秒。建议大于等于TTL/2。|300|
|TTL|服务器缓存解析记录的时长，单位秒，普通用户最小为600。|600|
|TIMEZONE|输出日志时的时区，单位小时。|8|
|TYPE|需要更改的记录类型，可以用“,”隔开，只能是“A”、“AAAA”或“A,AAAA”。|A,AAAA|
|CNIPV4|检查IPv4地址时，仅使用中国服务器。|false|
|WEBHOOK|WEBHOOK推送地址。|无|
|CHECKLOCAL|是否检查本地网卡IP。此选项将禁用在线API的IP检查。<br>网络模式必须设置为host。<br>(Windows版docker无法读取本机IP)|false|
|IPV4NETS|本地网卡的IPv4网段。格式示例：“192.168.1.0/24”。多个网段用“,”隔开。|无|
|IPV6NETS|本地网卡的IPv6网段。格式示例：“240e::/16”。多个网段用“,”隔开。|无|

以上环境变量均存在默认值，添加需要修改的环境变量即可。

### 命令行
##### 查看帮助信息
```
dotnet aliyun-ddns.dll --help
```
##### 查看版本信息
```
dotnet aliyun-ddns.dll --version
```
##### 运行
```
dotnet aliyun-ddns.dll \
    -u "ALIYUN's AccessKey-ID" \
    -p "ALIYUN's AccessKey-Secret" \
    -d "ddns.aliyun.win,ddns2.aliyun2.win" \
    -i 300 \
    -t 600 \
    --timezone 8.0 \
    --type A \
    --cnipv4
```

| 参数名称 | 注释 | 默认值 |
| :---- | :----- | :--- |
|u|阿里云的Access Key ID。[获取阿里云AccessToken](https://usercenter.console.aliyun.com/)|access key id|
|p|阿里云的Access Key Secret。|access key secret|
|d|需要更新的域名，多个域名需要“,”分隔。|my.domain.com|
|i|更新间隔，单位秒。建议大于等于TTL/2。|300|
|t|服务器缓存解析记录的时长，单位秒，普通用户最小为600。|600|
|timezone|输出日志时的时区，单位小时。|8|
|type|需要更改的记录类型，可以用“,”隔开，只能是“A”、“AAAA”或“A,AAAA”。|A,AAAA|
|cnipv4|检查IPv4地址时，仅使用中国服务器。|false|
|webhook|WEBHOOK推送地址。|无|
|checklocal|是否检查本地网卡IP。此选项将禁用在线API的IP检查。|false|
|ipv4nets|本地网卡的IPv4网段。格式示例：“192.168.1.0/24”。多个网段用“,”隔开。|无|
|ipv6nets|本地网卡的IPv6网段。格式示例：“240e::/16”。多个网段用“,”隔开。|无|

以上参数均存在默认值，添加需要修改的参数即可。
