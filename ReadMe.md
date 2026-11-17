# 阿里云 DDNS

[![Build, test and release](https://github.com/sanjusss/aliyun-ddns/actions/workflows/dotnet-core.yml/badge.svg?branch=master)](https://github.com/sanjusss/aliyun-ddns/actions/workflows/dotnet-core.yml)

一个基于 .NET 10 的阿里云解析动态域名客户端。支持公网 IPv4/IPv6、本地网卡地址、多目标、多线路、WebHook，以及 Docker 多架构部署。

## 默认行为与写入保护

- 无效、回环、内网或链路本地地址不能用于公网记录。
- 获取不到有效 IP 时，不查询或修改 DNS。
- 为兼容旧版，默认 `ConflictPolicy=Replace`：多条同类型记录会保留一条并删除多余记录，CNAME 冲突会被替换。
- 只有 `ConflictPolicy=Replace` 才允许生成和执行删除操作；执行层会再次检查删除授权。显式设置为 `Skip` 可禁止删除。
- `DryRun` 只输出计划，不执行 DNS 写操作，也不发送变更通知。
- 单个目标失败不会阻止其他目标更新。
- 日志使用结构化 JSON；AccessKey 和 WebHook 地址不会写入应用日志。

## 架构

```text
JSON / 环境变量 / 命令行 / 旧环境变量
                  ↓
          配置绑定与启动校验
                  ↓
       公网或本地 IP 获取与地址校验
                  ↓
       主域名解析与阿里云记录查询
                  ↓
        DdnsUpdatePlanner 生成计划
                  ↓
       删除授权、DryRun、执行层校验
                  ↓
          执行更新并发送成功通知
```

生产代码位于 `src/AliyunDdns`，测试位于 `tests/AliyunDdns.Tests`。阿里云 SDK 类型只在 `Infrastructure/Aliyun` 中使用；更新计划器不访问网络，也不读取环境变量。

## 快速开始

### 1. 准备 RAM 权限

建议使用仅用于 DDNS 的 RAM 用户，不要使用主账号 AccessKey。默认 `ConflictPolicy=Replace` 需要删除权限；显式设置为 `Skip` 时可移除删除权限。

以下策略是示例，请替换账号 ID 和域名：

```json
{
  "Version": "1",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "alidns:DescribeDomains",
        "alidns:DescribeSubDomainRecords"
      ],
      "Resource": "*"
    },
    {
      "Effect": "Allow",
      "Action": [
        "alidns:AddDomainRecord",
        "alidns:UpdateDomainRecord",
        "alidns:DeleteDomainRecord"
      ],
      "Resource": "acs:alidns::1234567890123456:domain/example.com"
    }
  ]
}
```

默认 `ConflictPolicy=Replace` 使用上述 `alidns:DeleteDomainRecord` 权限替换冲突和多余记录；如将策略改为 `Skip`，可从 RAM 策略中移除该权限。

### 2. 创建配置文件

复制 [示例配置](examples/aliyun-ddns.json)，修改 AccessKey ID、域名和网卡。每项说明如下：

| 配置 | 说明 | 默认值 |
| --- | --- | --- |
| `Updater:IntervalSeconds` | 更新间隔，最小 5 秒 | `300` |
| `Updater:DryRun` | 是否只输出计划 | `false` |
| `Updater:ConflictPolicy` | `Skip` 跳过冲突；`Replace` 允许删除冲突和多余记录 | `Replace` |
| `Updater:RequestTimeoutSeconds` | 单个公网 IP 或阿里云请求超时 | `10` |
| `Updater:RetryMaxAttempts` | 阿里云只读和更新请求的最大尝试次数，包含首次请求；允许 1–5 | `3` |
| `Updater:RetryBaseDelayMilliseconds` | 指数退避的首次等待时间，后续等待依次翻倍 | `500` |
| `Updater:LogTimeZoneOffsetHours` | 每轮结果和通知使用的整分钟时区偏移，例如 `8.5`；JSON 运行日志使用 UTC | `8` |
| `Updater:ChinaIPv4Only` | IPv4 只使用中国区域来源 | `false` |
| `Aliyun:AccessKeyId` | RAM 用户 AccessKey ID | 空 |
| `Aliyun:AccessKeySecret` | AccessKey Secret；推荐使用文件代替 | 空 |
| `Aliyun:AccessKeySecretFile` | Secret 文件路径，优先于直接配置 | 空 |
| `Aliyun:Endpoint` | 阿里云 DNS OpenAPI Endpoint | `alidns.aliyuncs.com` |
| `Webhook:Url` | 变更通知地址，留空禁用 | 空 |
| `Webhook:TimeoutSeconds` | 通知超时 | `10` |
| `Targets` | 解析目标数组，每项独立处理 | 空 |

每个目标支持：

| 配置 | 说明 |
| --- | --- |
| `Domain` | 完整域名，例如 `home.example.com`；允许最左侧为 `*.` 的泛解析目标 |
| `RootDomain` | 可选普通主域名，不允许通配符；留空时从阿里云托管域名中选择最长匹配后缀 |
| `Types` | `A`、`AAAA` 或二者；未配置时默认双栈，显式配置一种类型时不会追加另一种 |
| `Line` | 阿里云解析线路，默认 `default` |
| `Ttl` | 记录 TTL，默认 600 秒；普通套餐通常最低 600 秒，以账号套餐为准 |
| `Source` | `Public` 公网接口或 `Local` 本地网卡 |
| `Interface` | 本地模式的可选网卡名，例如 `eth0` |
| `Networks` | 可选 CIDR 数组，只保留网段内地址 |
| `AllowAnyInterface` | 是否绕过自动接口名过滤，允许容器网桥或特殊接口，默认 `false` |
| `AddressPolicy` | `Stable` 稳定地址优先、`Eui64` EUI-64 优先、`First` 按确定性顺序取第一项 |

本地目标未设置 `Interface` 或 `Networks` 时仍可运行，但默认排除 `docker*`、`br-*`、`veth*`、`vbr*`、`virbr*` 等常见容器网桥，避免按名称排序时误选其地址。普通 `eth*`、`ens*` 以及 `tun*`、`tap*`、`wg*`、`ppp*` 等 VPN 接口会保留，不按虚拟类型一律封禁。确需容器网桥或其他被过滤接口时，可显式设置 `AllowAnyInterface=true` 绕过名称过滤。旧 `CHECKLOCAL` 未配置网段时也使用相同的自动过滤规则。

Linux 通过 Netlink 读取 IPv6 临时地址、弃用标志和生命周期，Windows 使用系统地址元数据；这些信息会参与 `Stable`、`Eui64` 选择并过滤弃用地址。

### 3. 保存 Secret

Secret 文件只能包含 AccessKey Secret 本身。直接运行时，文件权限建议设为仅当前用户可读写，并放在 `secrets/` 或其他已忽略目录中，不要提交到 Git。使用下方 Docker 绑定挂载时，配置和 Secret 还必须允许容器内的 UID `1654` 读取；可以执行 `sudo chown 1654:1654 secrets/aliyun_access_key_secret` 后保持 `0600` 权限。

```json
{
  "Aliyun": {
    "AccessKeyId": "RAM 用户的 AccessKey ID",
    "AccessKeySecretFile": "/run/secrets/aliyun_access_key_secret"
  }
}
```

## 运行

### 命令行

发布目录中执行：

```bash
# 查看帮助和版本，不读取配置
dotnet aliyun-ddns.dll --help
dotnet aliyun-ddns.dll --version

# 只校验配置，不访问外部服务
dotnet aliyun-ddns.dll --config /path/aliyun-ddns.json --validate-config

# 只执行一轮
dotnet aliyun-ddns.dll --config /path/aliyun-ddns.json --once

# 输出计划，不执行 DNS 写操作
dotnet aliyun-ddns.dll --config /path/aliyun-ddns.json --once --dry-run

# 持续运行
dotnet aliyun-ddns.dll --config /path/aliyun-ddns.json
```

显式 `--config` 的相对路径按当前工作目录解析；未指定时仍读取发布目录下的 `appsettings.json`。已知长参数支持 `--name=value`，布尔开关支持 `=true` 和 `=false`，例如 `--dry-run=true`。未知开关和位置参数会返回退出码 `2`，新版 `--节:键=值` 继续可用。

命令行也可以覆盖普通配置：

```bash
dotnet aliyun-ddns.dll --Updater:IntervalSeconds=600 --once --dry-run
```

退出码：`0` 成功，`1` 更新或通知失败，`2` 配置错误，`130` 已取消。持续运行时，单轮失败会记录日志并等待下一轮。

### 新版环境变量

使用双下划线表示配置层级：

```bash
export Aliyun__AccessKeyId="RAM 用户的 AccessKey ID"
export Aliyun__AccessKeySecretFile="/run/secrets/aliyun_access_key_secret"
export Updater__IntervalSeconds="600"
export Targets__0__Domain="home.example.com"
export Targets__0__RootDomain="example.com"
export Targets__0__Types__0="A"
```

优先级为：默认配置 < JSON < 环境变量 < 命令行 < `--dry-run` 开关。旧环境变量只补充没有显式配置的新值。

### Docker

发布镜像同时推送到 Docker Hub 和 GitHub Container Registry（GHCR），可按网络情况选择：

```bash
docker pull sanjusss/aliyun-ddns:latest
docker pull ghcr.io/sanjusss/aliyun-ddns:latest
```

从仓库根目录构建：

```bash
docker build -t aliyun-ddns:local .
```

公网 IP 模式：

```bash
docker run -d \
  --name aliyun-ddns \
  --restart unless-stopped \
  -v "$PWD/aliyun-ddns.json:/app/appsettings.json:ro" \
  -v "$PWD/secrets/aliyun_access_key_secret:/run/secrets/aliyun_access_key_secret:ro" \
  aliyun-ddns:local
```

本地网卡模式需要观察宿主机地址，增加 `--network host`。镜像使用 UID `1654` 的非 root 用户运行，并包含 `LICENSE`。

Docker 一次性校验和 DryRun：

```bash
docker run --rm \
  -v "$PWD/aliyun-ddns.json:/app/appsettings.json:ro" \
  -v "$PWD/secrets/aliyun_access_key_secret:/run/secrets/aliyun_access_key_secret:ro" \
  aliyun-ddns:local --validate-config

docker run --rm --network host \
  -v "$PWD/aliyun-ddns.json:/app/appsettings.json:ro" \
  -v "$PWD/secrets/aliyun_access_key_secret:/run/secrets/aliyun_access_key_secret:ro" \
  aliyun-ddns:local --once --dry-run
```

## 旧环境变量迁移

旧变量仍可读取，启动时会输出弃用提示。新配置不会被旧变量覆盖。未设置 `TIMEZONE` 时默认使用 `8`；旧版没有冲突策略变量，升级后未显式配置时默认使用 `ConflictPolicy=Replace`。

| 旧变量 | 新配置 |
| --- | --- |
| `AKID` | `Aliyun:AccessKeyId` |
| `AKSCT` | `Aliyun:AccessKeySecret`；推荐改用 `AccessKeySecretFile` |
| `DOMAIN` | `Targets[*]:Domain`；旧的 `线路:域名` 写法仍可转换 |
| `ROOT_DOMAIN` | `Targets[*]:RootDomain` |
| `REDO` | `Updater:IntervalSeconds` |
| `TTL` | `Targets[*]:Ttl` |
| `TIMEZONE` | `Updater:LogTimeZoneOffsetHours` |
| `TYPE` | `Targets[*]:Types` |
| `CNIPV4` | `Updater:ChinaIPv4Only` |
| `WEBHOOK` | `Webhook:Url` |
| `CHECKLOCAL=true` | `Targets[*]:Source=Local` |
| `IPV4NETS` | 合并到 `Targets[*]:Networks` |
| `IPV6NETS` | 合并到 `Targets[*]:Networks` |

### 旧命令行参数

`0.3.x` 暂时保留旧命令行参数，并在启动时输出弃用提示；后续版本将移除。旧参数只补充没有显式配置的新值，Secret 仍推荐改用文件。长参数也可写成 `--domain=home.example.com`、`--type=AAAA` 等等号形式。

| 旧参数 | 对应旧变量 |
| --- | --- |
| `-u`, `--access-key-id` | `AKID` |
| `-p`, `--access-key-secret` | `AKSCT` |
| `-d`, `--domain` | `DOMAIN` |
| `--root-domain` | `ROOT_DOMAIN` |
| `-i`, `--interval` | `REDO` |
| `-t`, `--ttl` | `TTL` |
| `--timezone` | `TIMEZONE` |
| `--type` | `TYPE` |
| `--cnipv4` | `CNIPV4=true` |
| `-h`, `--webhook` | `WEBHOOK` |
| `-c`, `--checklocal` | `CHECKLOCAL=true` |
| `--ipv4nets` | `IPV4NETS` |
| `--ipv46nets`, `--ipv6nets` | `IPV6NETS` |

为保持旧行为，`-h` 仍表示 WebHook；帮助参数是 `--help`。例如：

```bash
dotnet aliyun-ddns.dll -u "RAM AccessKey ID" -p "RAM AccessKey Secret" \
  -d "home.example.com" --root-domain example.com -i 300 -t 600 --once --dry-run
```

该示例仅说明参数迁移。命令行 Secret 可能进入 shell 历史或进程参数，正式使用请改用 `Aliyun:AccessKeySecretFile`。

## 重试行为

阿里云的托管域名查询、解析记录查询和按 `RecordId` 更新采用有限重试：默认最多尝试 3 次，可将 `RetryMaxAttempts` 配置为 1–5 次，首次退避 500 毫秒，之后按 2 倍递增。HTTP 429、5xx、明确的网络异常、请求超时，以及 `TeaException.Code` 包含 `Throttling` 的 HTTP 400 限流错误会触发重试；其他认证、参数等 4xx 错误不重试，取消操作会立即中断退避等待。

新增和删除操作的结果在超时后可能无法确定，因此不会自动重试，避免重复新增或重复删除。它们会在下一轮更新时重新协调。公网 IP 查询使用多个 HTTPS 来源依次回退，不对同一来源重复请求。

## 删除与冲突规则

为兼容旧版，未显式配置 `Updater:ConflictPolicy` 时默认使用 `Replace`。如果不希望程序删除 DNS 记录，必须显式设置为 `Skip`。

`ConflictPolicy=Skip` 时：

- 多条同类型记录：跳过该类型。
- CNAME 冲突：跳过请求的类型。
- 不执行任何删除。

`ConflictPolicy=Replace` 时：

- 多条同类型记录：按 `RecordId` 排序，更新第一条，再删除多余记录。
- CNAME 冲突：先删除 CNAME，再新增或更新目标类型。
- IP 无效时仍然禁止所有写操作。

`Replace` 会删除真实 DNS 记录，应同时启用最小 RAM 权限，并先用 `--once --dry-run` 检查计划。

## 开发、检查和发布

```bash
dotnet restore aliyun-ddns.sln --locked-mode
dotnet format aliyun-ddns.sln --verify-no-changes --no-restore
dotnet build aliyun-ddns.sln --configuration Release --no-restore
dotnet test aliyun-ddns.sln --configuration Release --no-build --no-restore
dotnet list aliyun-ddns.sln package --vulnerable --include-transitive
```

测试中的 DNS、HTTP 和 WebHook 全部使用 fake，不访问真实服务。版本统一来自 `Directory.Build.props`。Git 标签发布必须使用 `X.Y.Z`，CI 会覆盖程序版本并校验 `--version` 输出。

CI 在 PR 上只构建和测试。发布 job 仅允许 `master` 或合法 `X.Y.Z` 标签，统一使用受保护的 `production` environment 和全局串行 `concurrency`；应在 GitHub 为该 environment 仅配置允许 `master` 和合法版本标签的部署限制，不配置必需审核者，workflow 满足条件后自动发布。`master` 更新 Docker Hub 和 GHCR 的 `latest`，普通标签只推送版本标签；只有手动在合法标签上明确启用 `publish_latest` 才同时更新 `latest`，避免历史标签覆盖最新镜像。

GHCR 使用 `GITHUB_TOKEN` 和 `packages: write`，不需要额外 PAT。首次发布后应在 [GHCR 包页面](https://github.com/users/sanjusss/packages/container/package/aliyun-ddns) 确认包已关联 `sanjusss/aliyun-ddns`；如需匿名拉取，还要在 Package settings 中将可见性设置为 Public。

NuGet 审计覆盖直接和传递依赖，`NU1901`–`NU1904` 作为构建错误，显式漏洞列表检查继续保留。发布镜像包含 amd64、arm64、arm/v7、SBOM 和 provenance。标签发布同时生成包含 `LICENSE` 的 ZIP 和 SHA-256 文件。Docker SDK/runtime 基础镜像固定多架构 manifest digest，Dependabot 每周检查更新。

## 许可证

[BSD 3-Clause License](LICENSE)
