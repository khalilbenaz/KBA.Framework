FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["KBA.TenantService/KBA.TenantService.csproj", "KBA.TenantService/"]
COPY ["../../src/KBA.Framework.Domain/KBA.Framework.Domain.csproj", "KBA.Framework.Domain/"]

RUN dotnet restore "KBA.TenantService/KBA.TenantService.csproj"

COPY KBA.TenantService/ KBA.TenantService/
COPY ../../src/KBA.Framework.Domain/ KBA.Framework.Domain/

WORKDIR "/src/KBA.TenantService"
RUN dotnet build "KBA.TenantService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KBA.TenantService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KBA.TenantService.dll"]
