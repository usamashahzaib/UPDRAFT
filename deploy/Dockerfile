# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src

COPY Backend/HotdropBackend/HotdropBackend.csproj Backend/HotdropBackend/
RUN dotnet restore Backend/HotdropBackend/HotdropBackend.csproj --arch ${TARGETARCH}

COPY Backend/HotdropBackend/ Backend/HotdropBackend/
COPY docs/legal/account-deletion.html docs/legal/account-deletion.html
RUN dotnet publish Backend/HotdropBackend/HotdropBackend.csproj \
    --configuration Release \
    --output /app/publish \
    --arch ${TARGETARCH} \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

COPY --from=build /app/publish .

USER $APP_UID
ENTRYPOINT ["dotnet", "HotdropBackend.dll"]
