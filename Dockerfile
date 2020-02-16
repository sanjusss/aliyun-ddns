
ARG runtime=3.1-runtime
ARG version=0

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
MAINTAINER sanjusss <sanjusss@qq.com>
WORKDIR /src
COPY . /src
RUN dotnet publish -c Release -o /app --version-suffix=$version ./aliyun-ddns/aliyun-ddns.csproj

FROM mcr.microsoft.com/dotnet/core/runtime:$runtime AS final
MAINTAINER sanjusss <sanjusss@qq.com>
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "aliyun-ddns.dll"]
