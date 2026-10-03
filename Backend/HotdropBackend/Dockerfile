FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Backend/HotdropBackend/HotdropBackend.csproj Backend/HotdropBackend/
COPY docs/legal/account-deletion.html docs/legal/account-deletion.html
RUN dotnet restore Backend/HotdropBackend/HotdropBackend.csproj
COPY Backend/HotdropBackend/ Backend/HotdropBackend/
RUN dotnet publish Backend/HotdropBackend/HotdropBackend.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "HotdropBackend.dll"]
