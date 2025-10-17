FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["KBA.ApiGateway/KBA.ApiGateway.csproj", "KBA.ApiGateway/"]

RUN dotnet restore "KBA.ApiGateway/KBA.ApiGateway.csproj"

COPY KBA.ApiGateway/ KBA.ApiGateway/

WORKDIR "/src/KBA.ApiGateway"
RUN dotnet build "KBA.ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KBA.ApiGateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KBA.ApiGateway.dll"]
