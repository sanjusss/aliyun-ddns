
FROM mcr.microsoft.com/dotnet/core/runtime:3.1-alpine AS final
MAINTAINER sanjusss <sanjusss@qq.com>
WORKDIR /app
COPY ./out .
ENTRYPOINT ["dotnet", "aliyun-ddns.dll"]
