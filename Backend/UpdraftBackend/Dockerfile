FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Backend/UpdraftBackend/UpdraftBackend.csproj Backend/UpdraftBackend/
COPY docs/legal/account-deletion.html docs/legal/account-deletion.html
RUN dotnet restore Backend/UpdraftBackend/UpdraftBackend.csproj
COPY Backend/UpdraftBackend/ Backend/UpdraftBackend/
RUN dotnet publish Backend/UpdraftBackend/UpdraftBackend.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "UpdraftBackend.dll"]
