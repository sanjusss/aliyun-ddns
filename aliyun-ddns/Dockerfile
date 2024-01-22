FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
MAINTAINER sanjusss <sanjusss@qq.com>
WORKDIR /src
COPY . /src
RUN dotnet publish -c Release -o /app /src/aliyun-ddns/

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS final
MAINTAINER sanjusss <sanjusss@qq.com>
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "aliyun-ddns.dll"]
