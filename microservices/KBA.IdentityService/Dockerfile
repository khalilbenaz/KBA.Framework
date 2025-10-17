FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copier les fichiers de projet
COPY ["KBA.IdentityService/KBA.IdentityService.csproj", "KBA.IdentityService/"]
COPY ["../../src/KBA.Framework.Domain/KBA.Framework.Domain.csproj", "KBA.Framework.Domain/"]

# Restaurer les dépendances
RUN dotnet restore "KBA.IdentityService/KBA.IdentityService.csproj"

# Copier tout le code
COPY KBA.IdentityService/ KBA.IdentityService/
COPY ../../src/KBA.Framework.Domain/ KBA.Framework.Domain/

# Build
WORKDIR "/src/KBA.IdentityService"
RUN dotnet build "KBA.IdentityService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KBA.IdentityService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KBA.IdentityService.dll"]
