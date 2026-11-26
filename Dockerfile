ARG BUILDPLATFORM
# SDK 仅在构建机原生平台运行；发布产物为跨架构托管 DLL。
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props ./
COPY src/AliyunDdns/AliyunDdns.csproj src/AliyunDdns/packages.lock.json src/AliyunDdns/
RUN dotnet restore src/AliyunDdns/AliyunDdns.csproj --locked-mode -p:NuGetAudit=false

COPY src/AliyunDdns/ src/AliyunDdns/
COPY LICENSE ./LICENSE
ARG VERSION=0.3.0
RUN dotnet publish src/AliyunDdns/AliyunDdns.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:Version=${VERSION}

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
ARG VERSION=0.3.0
LABEL org.opencontainers.image.title="aliyun-ddns" \
      org.opencontainers.image.description="阿里云解析动态域名客户端" \
      org.opencontainers.image.version="${VERSION}" \
      org.opencontainers.image.source="https://github.com/sanjusss/aliyun-ddns" \
      org.opencontainers.image.url="https://github.com/sanjusss/aliyun-ddns" \
      org.opencontainers.image.licenses="BSD-3-Clause"
WORKDIR /app
COPY --from=build --chown=1654:1654 /app/publish .
COPY --from=build --chown=1654:1654 /src/LICENSE ./LICENSE
USER 1654
ENTRYPOINT ["dotnet", "aliyun-ddns.dll"]
