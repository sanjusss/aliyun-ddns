
FROM mcr.microsoft.com/dotnet/runtime:6.0 AS final
LABEL maintainer="sanjusss@qq.com"
ENV AKID="access key id"
ENV AKSCT="access key secret"
ENV DOMAIN="my.domain.com"
ENV REDO=300
ENV TTL=600
ENV TIMEZONE=8
ENV TYPE=A,AAAA
ENV CNIPV4=false
ENV WEBHOOK=
ENV CHECKLOCAL=false
ENV IPV4NETS=
ENV IPV6NETS=
WORKDIR /app
COPY ./out .
ENTRYPOINT ["dotnet", "aliyun-ddns.dll"]
