#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-alpine AS base
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-alpine AS build
WORKDIR /src
COPY ["EasyKubeLogService/EasyKubeLogService.csproj", "EasyKubeLogService/"]

RUN dotnet restore "EasyKubeLogService/EasyKubeLogService.csproj"
COPY . .
WORKDIR "/src/EasyKubeLogService"
RUN dotnet build "EasyKubeLogService.csproj" -c Release -o /app/build &&\
    dotnet publish "EasyKubeLogService.csproj" -c Release -o /app/publish
    #dotnet publish "EasyKubeLogService.csproj" -c Release -r:linux-x64 --self-contained -p:PublishTrimmed=true -o /app/publish


FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS https://+:5001
ENTRYPOINT ["./EasyKubeLogService", "Kestrel:Certificates:Default:Path=/app/Cert/localhost.pfx", "Kestrel:Certificates:Default:Password=easykubelogservicepassword"]


#FROM alpine:3.9.4 AS base
#WORKDIR /app
#EXPOSE 5001
#RUN apk add --no-cache \ 
    #openssh libunwind \
    #nghttp2-libs libidn krb5-libs libuuid lttng-ust zlib \
    #libstdc++ libintl \
    #icu

#FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
#FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
