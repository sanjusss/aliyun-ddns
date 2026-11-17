# 公网 IP 来源

本文列出程序默认使用的公网 IP 查询接口，对应代码位于 `src/AliyunDdns/Infrastructure/Ip/IpSourceCatalog.cs`。

测试时间：2026-11-17。测试网络：江苏移动家宽、腾讯云上海（IPv4 与 IPv6 双栈）。

## IPv4

| 名称 | 地址 | 返回格式 | 区域 | 说明 |
| --- | --- | --- | --- | --- |
| `ipip-ipv4` | https://myip.ipip.net/json | JSON，字段 `data.ip` | 中国 | IPIP 官方服务 |
| `oray-ipv4` | https://ddns.oray.com/checkip | 文本，从响应中提取 IP | 中国 | 花生壳官方 DDNS |
| `qqnews-ipv4` | https://i.news.qq.com/api/ip2city | JSON，字段 `ip` | 中国 | 腾讯新闻业务接口 |
| `yinghualuo-ipv4` | https://ip.yinghualuo.cn/bejson | JSON，字段 `ip` | 中国 | 个人公益服务 |
| `aws-checkip-ipv4` | https://checkip.amazonaws.com/ | 纯文本 | 全球 | AWS |
| `icanhazip-ipv4` | https://ipv4.icanhazip.com/ | 纯文本 | 全球 | Cloudflare |
| `ip-sb-ipv4` | https://api.ip.sb/ip | 纯文本 | 全球 | IP.SB |
| `ifconfig-me-ipv4` | https://ifconfig.me/ip | 纯文本 | 全球 | ifconfig.me |

## IPv6

| 名称 | 地址 | 返回格式 | 区域 | 说明 |
| --- | --- | --- | --- | --- |
| `dnspod-ipv6` | https://ipv6.ddnspod.com/ | 纯文本 | 中国 | 腾讯 DNSPod |
| `ipify-ipv6` | https://api6.ipify.org/ | 纯文本 | 全球 | ipify |
| `icanhazip-ipv6` | https://ipv6.icanhazip.com/ | 纯文本 | 全球 | Cloudflare |
| `ident-me-ipv6` | https://v6.ident.me/ | 纯文本 | 全球 | ident.me |
| `ip-sb-ipv6` | https://api-ipv6.ip.sb/ip | 纯文本 | 全球 | IP.SB |

## 查询顺序与回退

- 每轮按表内顺序尝试，取到合法公网地址后停止。
- 请求失败、超时、响应过大、内容无法解析或地址校验失败时，继续尝试下一个来源；同一来源不重试。
- 所有来源都失败时，本轮跳过该目标的 DNS 写操作。
- `Updater:ChinaIPv4Only=true` 时，IPv4 只使用区域为中国的来源。

## 已移除的来源

| 地址 | 移除原因 |
| --- | --- |
| https://4.ipw.cn/ | 域名无 A/AAAA 记录，服务停运 |
| https://6.ipw.cn/ | 域名无 A/AAAA 记录，服务停运 |
| https://api.ipify.org/ | 家宽和腾讯云直连均失败，国内不可用 |
| https://ip.3322.net/ | 证书 2026-11-14 过期，未续期 |
| https://myip4.ipip.net/ | 域名无解析 |
| https://api.myip.com/ | 请求超时 |
| https://ipapi.co/ip/ | 返回 429 限流 |

## 维护说明

- 来源必须使用 HTTPS，单次响应不超过 1024 字节。
- 业务接口和个人服务可能随时变更、限流或下线，建议定期复查。
- `ip.3322.net` 证书续期后可以重新评估是否加入。
- 调整来源列表时同步修改 `IpSourceCatalog` 和 `PublicIpAddressProviderTests`。