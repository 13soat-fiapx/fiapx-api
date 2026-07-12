# syntax=docker/dockerfile:1.20

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

COPY Directory.Build.props .
COPY --parents src/**/*.csproj src/**/packages.lock.json ./
RUN dotnet restore src/FiapX.Api/FiapX.Api.csproj --locked-mode

COPY src src
RUN dotnet publish src/FiapX.Api/FiapX.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble-chiseled AS runtime
WORKDIR /app

ENV LANG=pt_BR.UTF-8 LANGUAGE=pt_BR:pt LC_ALL=pt_BR.UTF-8
ENV TZ=America/Sao_Paulo
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FiapX.Api.dll"]
