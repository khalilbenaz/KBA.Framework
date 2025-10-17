FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["KBA.ProductService/KBA.ProductService.csproj", "KBA.ProductService/"]
COPY ["../../src/KBA.Framework.Domain/KBA.Framework.Domain.csproj", "KBA.Framework.Domain/"]

RUN dotnet restore "KBA.ProductService/KBA.ProductService.csproj"

COPY KBA.ProductService/ KBA.ProductService/
COPY ../../src/KBA.Framework.Domain/ KBA.Framework.Domain/

WORKDIR "/src/KBA.ProductService"
RUN dotnet build "KBA.ProductService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KBA.ProductService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KBA.ProductService.dll"]
