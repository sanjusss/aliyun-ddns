ARG BUILDPLATFORM
# SDK 仅在构建机原生平台运行；发布产物为跨架构托管 DLL。
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:11.0@sha256:65f3c7c6a25a239ab3e6719f2896147e7b68242ff2d8818dd70498bde6ef8bea AS build
WORKDIR /src

COPY global.json Directory.Build.props ./
COPY src/AliyunDdns/AliyunDdns.csproj src/AliyunDdns/packages.lock.json src/AliyunDdns/
RUN --mount=type=cache,id=aliyun-ddns-nuget,target=/root/.nuget/packages \
    dotnet restore src/AliyunDdns/AliyunDdns.csproj --locked-mode -p:NuGetAudit=false

COPY src/AliyunDdns/ src/AliyunDdns/
COPY LICENSE ./LICENSE
ARG VERSION=0.3.0
RUN --mount=type=cache,id=aliyun-ddns-nuget,target=/root/.nuget/packages \
    dotnet publish src/AliyunDdns/AliyunDdns.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:Version=${VERSION}

FROM mcr.microsoft.com/dotnet/runtime:10.0@sha256:80a51954ac70925e5783cb1ed35129a4243dcb1cddef20066b67346e17db284d AS final
WORKDIR /app
COPY --from=build --chown=1654:1654 /app/publish .
COPY --from=build --chown=1654:1654 /src/LICENSE ./LICENSE
USER 1654
ENTRYPOINT ["dotnet", "aliyun-ddns.dll"]
