FROM microsoft/dotnet:3.0-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:3.0-sdk AS build
WORKDIR /src
COPY aliyun-ddns/aliyun-ddns.csproj aliyun-ddns/
RUN dotnet restore aliyun-ddns/aliyun-ddns.csproj
COPY . .
WORKDIR /src/aliyun-ddns
RUN dotnet build aliyun-ddns.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish aliyun-ddns.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "aliyun-ddns.dll"]
