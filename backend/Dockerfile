# BBF Firmas e Poderes — API + Worker (WF-19)
# targets: api | worker

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY global.json ./
COPY src/BbfFirmasPoderes.Domain/BbfFirmasPoderes.Domain.csproj src/BbfFirmasPoderes.Domain/
COPY src/BbfFirmasPoderes.Infrastructure/BbfFirmasPoderes.Infrastructure.csproj src/BbfFirmasPoderes.Infrastructure/
COPY src/BbfFirmasPoderes.Api/BbfFirmasPoderes.Api.csproj src/BbfFirmasPoderes.Api/
COPY src/BbfFirmasPoderes.Worker/BbfFirmasPoderes.Worker.csproj src/BbfFirmasPoderes.Worker/

RUN dotnet restore src/BbfFirmasPoderes.Api/BbfFirmasPoderes.Api.csproj \
 && dotnet restore src/BbfFirmasPoderes.Worker/BbfFirmasPoderes.Worker.csproj

COPY src/ ./src/

RUN dotnet publish src/BbfFirmasPoderes.Api/BbfFirmasPoderes.Api.csproj \
        -c Release -o /app/api --no-restore \
 && dotnet publish src/BbfFirmasPoderes.Worker/BbfFirmasPoderes.Worker.csproj \
        -c Release -o /app/worker --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS api
WORKDIR /app
USER root
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/* \
 && mkdir -p /data/docs \
 && chown -R $APP_UID:$APP_UID /data/docs
COPY --from=build --chown=$APP_UID:$APP_UID /app/api ./
USER $APP_UID
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
HEALTHCHECK --interval=10s --timeout=5s --start-period=40s --retries=12 \
  CMD curl -fsS http://127.0.0.1:8080/health/ready || exit 1
ENTRYPOINT ["dotnet", "BbfFirmasPoderes.Api.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS worker
WORKDIR /app
USER root
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/* \
 && mkdir -p /data/docs \
 && chown -R $APP_UID:$APP_UID /data/docs
COPY --from=build --chown=$APP_UID:$APP_UID /app/worker ./
USER $APP_UID
EXPOSE 8081
ENV ASPNETCORE_HTTP_PORTS=8081
HEALTHCHECK --interval=10s --timeout=5s --start-period=25s --retries=12 \
  CMD curl -fsS http://127.0.0.1:8081/health/ready || exit 1
ENTRYPOINT ["dotnet", "BbfFirmasPoderes.Worker.dll"]
