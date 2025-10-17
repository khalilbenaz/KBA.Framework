FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy domain project
COPY ["microservices/KBA.Framework.Domain/KBA.Framework.Domain.csproj", "microservices/KBA.Framework.Domain/"]

# Copy permission service project
COPY ["microservices/KBA.PermissionService/KBA.PermissionService.csproj", "microservices/KBA.PermissionService/"]

# Restore
RUN dotnet restore "microservices/KBA.PermissionService/KBA.PermissionService.csproj"

# Copy all files
COPY . .

# Build
WORKDIR "/src/microservices/KBA.PermissionService"
RUN dotnet build "KBA.PermissionService.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "KBA.PermissionService.csproj" -c Release -o /app/publish

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KBA.PermissionService.dll"]
