
[![Build status](https://ci.appveyor.com/api/projects/status/r585ycrd9wn0v5ed?svg=true)](https://ci.appveyor.com/project/sanjusss/aliyun-ddns)
[![](https://img.shields.io/docker/stars/sanjusss/aliyun-ddns.svg?logo=docker)](https://hub.docker.com/r/sanjusss/aliyun-ddns)
[![GitHub license](https://img.shields.io/github/license/sanjusss/aliyun-ddns.svg)](https://github.com/sanjusss/aliyun-ddns/blob/master/LICENSE)  
[![GitHub tag](https://img.shields.io/github/tag/sanjusss/aliyun-ddns.svg)](https://github.com/sanjusss/aliyun-ddns/tags)
[![GitHub issues](https://img.shields.io/github/issues/sanjusss/aliyun-ddns.svg)](https://github.com/sanjusss/aliyun-ddns/issues)
[![GitHub forks](https://img.shields.io/github/forks/sanjusss/aliyun-ddns.svg)](https://github.com/sanjusss/aliyun-ddns/network)
[![GitHub stars](https://img.shields.io/github/stars/sanjusss/aliyun-ddns.svg)](https://github.com/sanjusss/aliyun-ddns/stargazers)


# 使用方法

### Docker
```
docker run -d --restart=always --net=host \
    -e "AKID=[ALIYUN's AccessKey-ID]" \
    -e "AKSCT=[ALIYUN's AccessKey-Secret]" \
    -e "DOMAIN=ddns.aliyun.win" \
    -e "ENDPOINT=cn-hangzhou" \
    -e "REDO=30" \
    -e "TTL=60" \
    -e "TIMEZONE=8.0" \
    -e "TYPE=A,AAAA" \
    sanjusss/aliyun-ddns
```
如果要支持IPv6,必须使用`--net=host`模式，否则无法设置宿主机的ipv6地址到AAAA记录。  
AKID：阿里云的Access Key ID。  
AKSCT：阿里云的Access Key Secret。  
DOMAIN：需要更新的域名，多个域名需要“,”分隔。  
ENDPOINT：默认为cn-hangzhou，[详见定义](https://help.aliyun.com/document_detail/40654.html?spm=a2c4e.11153987.0.0.6d85366aUfTWbG)。  
REDO： 更新间隔，单位秒。建议大于等于TTL/2。  
TTL： 服务器缓存解析记录的时长，单位秒，普通用户最小为600。  
TIMEZONE： 输出日志时的时区，单位小时。  
TYPE： 需要更改的记录类型，可以用“,”隔开，只能是“A”、“AAAA”或“A,AAAA”。

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
    -e "cn-hangzhou" \
    -i 300 \
    -t 600 \
    --timezone 8.0 \
    --type A
```
u：阿里云的Access Key ID。  
p：阿里云的Access Key Secret。  
d：需要更新的域名，多个域名需要“,”分隔。
e：默认为cn-hangzhou，[详见定义](https://help.aliyun.com/document_detail/40654.html?spm=a2c4e.11153987.0.0.6d85366aUfTWbG)。  
i： 更新间隔，单位秒。建议大于等于TTL/2。  
t： 服务器缓存解析记录的时长，单位秒，普通用户最小为600。  
tz： 输出日志时的时区，单位小时。  
type： 需要更改的记录类型，可以用“,”隔开，只能是“A”、“AAAA”或“A,AAAA”。  