#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["EasyLogService/EasyLogService.csproj", "EasyLogService/"]

RUN dotnet restore "EasyLogService/EasyLogService.csproj"
COPY . .
WORKDIR "/src/EasyLogService"
RUN dotnet build "EasyLogService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EasyLogService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS https://+:5001
#ENV ASPNETCORE_URLS http://+:5000
#RUN chmod +x EasyLogService
ENTRYPOINT ["/app/EasyLogService", "Kestrel:Certificates:Default:Path=/app/Cert/localhost.pfx", "Kestrel:Certificates:Default:Password=easylogservicepassword"]