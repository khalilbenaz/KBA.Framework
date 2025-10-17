# FAQ Complète - Architecture Microservices

## 1️⃣ Base de Données Unique vs Multiple

### Option A : Base de Données Unique (Votre préférence)

**Avantages** :
- ✅ Plus simple à gérer
- ✅ Pas de problème de cohérence des données
- ✅ Migrations plus faciles
- ✅ Idéal pour débuter avec les microservices

**Configuration** :

Modifiez les `appsettings.json` de tous les services pour utiliser la même base :

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=KBAFrameworkDb;Trusted_Connection=true;TrustServerCertificate=true"
  }
}
```

**⚠️ Important** : Chaque service garde ses propres tables avec des noms distincts :
- Identity Service : `KBA.Users`, `KBA.Roles`
- Product Service : `KBA.Products`
- Tenant Service : `KBA.Tenants`

### Option B : Bases de Données Séparées (Configuration actuelle)

**Avantages** :
- ✅ Isolation complète
- ✅ Scalabilité par service
- ✅ Pas de dépendances entre services

**Inconvénients** :
- ❌ Plus complexe
- ❌ Nécessite la cohérence éventuelle
- ❌ Plus de bases à gérer

## 2️⃣ Communication entre Microservices

### Synchrone (HTTP/REST)

#### Via l'API Gateway
```
Product Service → API Gateway → Identity Service
```

**Exemple** : Product Service vérifie si un utilisateur existe

```csharp
// Dans ProductService
public class ProductServiceLogic
{
    private readonly HttpClient _httpClient;
    
    public ProductServiceLogic(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("IdentityService");
    }
    
    public async Task<bool> ValidateUserAsync(Guid userId)
    {
        var response = await _httpClient.GetAsync($"http://localhost:5001/api/users/{userId}");
        return response.IsSuccessStatusCode;
    }
}
```

**Configuration dans Program.cs** :
```csharp
builder.Services.AddHttpClient("IdentityService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5001");
});
```

### Asynchrone (Message Queue)

#### Avec RabbitMQ (Recommandé)

**Installation** :
```powershell
# Dans chaque service .csproj
<PackageReference Include="RabbitMQ.Client" Version="6.8.1" />
<PackageReference Include="MassTransit" Version="8.1.3" />
<PackageReference Include="MassTransit.RabbitMQ" Version="8.1.3" />
```

**Configuration** :
```csharp
// Program.cs
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
    });
});
```

**Publier un événement** :
```csharp
// Identity Service - quand un utilisateur est créé
public class UserService
{
    private readonly IPublishEndpoint _publishEndpoint;
    
    public async Task CreateUserAsync(CreateUserDto dto)
    {
        var user = new User(...);
        await _context.SaveChangesAsync();
        
        // Publier l'événement
        await _publishEndpoint.Publish(new UserCreatedEvent
        {
            UserId = user.Id,
            Email = user.Email,
            TenantId = user.TenantId
        });
    }
}
```

**Consommer un événement** :
```csharp
// Product Service - écouter les événements
public class UserCreatedConsumer : IConsumer<UserCreatedEvent>
{
    public async Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        var userId = context.Message.UserId;
        // Faire quelque chose avec l'info...
    }
}

// Dans Program.cs
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<UserCreatedConsumer>();
    // ...
});
```

### Événements de Domaine

**Créer les événements partagés** dans `src/KBA.Framework.Domain/Events/` :

```csharp
// UserCreatedEvent.cs
namespace KBA.Framework.Domain.Events;

public record UserCreatedEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public Guid? TenantId { get; init; }
}

public record ProductCreatedEvent
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid? TenantId { get; init; }
}
```

## 3️⃣ Ajouter un Nouveau Microservice

### Exemple : Order Service

#### Étape 1 : Créer le projet

```powershell
cd microservices
dotnet new webapi -n KBA.OrderService
cd KBA.OrderService
```

#### Étape 2 : Créer le .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
    <PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\KBA.Framework.Domain\KBA.Framework.Domain.csproj" />
  </ItemGroup>
</Project>
```

#### Étape 3 : Program.cs

```csharp
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/order-service-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

// Database - Base unique !
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<IOrderService, OrderService>();

// JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["SecretKey"])),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"]
        };
    });

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
```

#### Étape 4 : Ajouter au Gateway

Modifiez `KBA.ApiGateway/ocelot.json` :

```json
{
  "Routes": [
    // ... routes existantes ...
    {
      "DownstreamPathTemplate": "/api/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "localhost",
          "Port": 5004
        }
      ],
      "UpstreamPathTemplate": "/api/orders/{everything}",
      "UpstreamHttpMethod": [ "GET", "POST", "PUT", "DELETE" ],
      "Key": "order-service"
    }
  ]
}
```

#### Étape 5 : Ajouter à la solution

```powershell
dotnet sln KBA.Microservices.sln add KBA.OrderService/KBA.OrderService.csproj
```

#### Étape 6 : Mettre à jour le script de démarrage

Dans `start-microservices.ps1`, ajoutez :

```powershell
Start-Service -ServiceName "Order Service" -ServicePath "$rootPath\KBA.OrderService" -Port 5004
```

## 4️⃣ Utilisation du Dossier src

Le dossier `src/` contient le **code partagé** entre les microservices :

### src/KBA.Framework.Domain/

**Contient** :
- ✅ Entités de domaine (User, Product, Tenant, Order, etc.)
- ✅ Interfaces de repositories
- ✅ Événements de domaine
- ✅ Value Objects
- ✅ Constantes (KBAConsts)

**Utilisé par** : TOUS les microservices via `ProjectReference`

**Exemple** :
```xml
<!-- Dans chaque microservice -->
<ItemGroup>
  <ProjectReference Include="..\..\src\KBA.Framework.Domain\KBA.Framework.Domain.csproj" />
</ItemGroup>
```

### src/KBA.Framework.Application/

**Option 1** : Ne plus l'utiliser (remplacé par les services dans chaque microservice)

**Option 2** : Le transformer en **package NuGet partagé** avec des utilitaires communs :
- DTOs de base
- Validators communs
- Helpers

### src/KBA.Framework.Infrastructure/

**Déconseillé** dans les microservices. Chaque service a sa propre infrastructure.

**Alternative** : Créer un package NuGet `KBA.Framework.Shared` avec :
- Extensions communes
- Middleware partagés
- Configurations communes

### src/KBA.Framework.Api/

**C'est l'ancien monolithe** - vous pouvez :
- ✅ Le garder pour les anciens clients
- ✅ Le migrer progressivement
- ✅ Le supprimer une fois la migration terminée

## 5️⃣ Débogage avec Microservices

### Option 1 : Visual Studio (Multi-Startup)

1. Clic droit sur la solution → **Set Startup Projects**
2. Sélectionnez **Multiple startup projects**
3. Configurez l'action **Start** pour :
   - KBA.ApiGateway
   - KBA.IdentityService
   - KBA.ProductService
   - KBA.TenantService

4. Appuyez sur **F5** → Tous les services démarrent avec le debugger !

### Option 2 : Visual Studio Code

**launch.json** :
```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Identity Service",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/microservices/KBA.IdentityService/bin/Debug/net8.0/KBA.IdentityService.dll",
      "cwd": "${workspaceFolder}/microservices/KBA.IdentityService"
    },
    {
      "name": "Product Service",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/microservices/KBA.ProductService/bin/Debug/net8.0/KBA.ProductService.dll",
      "cwd": "${workspaceFolder}/microservices/KBA.ProductService"
    }
  ],
  "compounds": [
    {
      "name": "All Microservices",
      "configurations": ["Identity Service", "Product Service", "Tenant Service", "API Gateway"]
    }
  ]
}
```

### Option 3 : Attach to Process

1. Démarrez les services normalement : `.\start-microservices.ps1`
2. Dans Visual Studio : **Debug → Attach to Process**
3. Filtrez par `dotnet.exe`
4. Attachez-vous aux processus des microservices

### Option 4 : Remote Debugging (Docker)

```dockerfile
# Dockerfile.debug
FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app
COPY . .
RUN dotnet publish -c Debug -o out

# Installer le debugger distant
RUN apt-get update && apt-get install -y unzip && \
    curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg

ENTRYPOINT ["dotnet", "out/KBA.IdentityService.dll"]
```

## 6️⃣ Déploiement Docker ET IIS

### Déploiement Docker

**docker-compose.yml** (déjà créé) :
```powershell
docker-compose up -d
```

**Avantages** :
- ✅ Isolation complète
- ✅ Portable
- ✅ Scalabilité facile

### Déploiement IIS (Tous les microservices)

#### Script PowerShell

```powershell
# deploy-microservices-iis.ps1

$services = @(
    @{Name="IdentityService"; Port=5001; Path="KBA.IdentityService"},
    @{Name="ProductService"; Port=5002; Path="KBA.ProductService"},
    @{Name="TenantService"; Port=5003; Path="KBA.TenantService"},
    @{Name="ApiGateway"; Port=5000; Path="KBA.ApiGateway"}
)

foreach ($service in $services) {
    Write-Host "Déploiement de $($service.Name)..." -ForegroundColor Green
    
    # Publier
    dotnet publish "microservices\$($service.Path)\$($service.Path).csproj" `
        -c Release `
        -o "C:\inetpub\wwwroot\KBA_$($service.Name)"
    
    # Créer le pool d'application
    New-WebAppPool -Name "KBA_$($service.Name)Pool"
    Set-ItemProperty "IIS:\AppPools\KBA_$($service.Name)Pool" `
        -Name "managedRuntimeVersion" -Value ""
    
    # Créer le site
    New-Website -Name "KBA_$($service.Name)" `
        -PhysicalPath "C:\inetpub\wwwroot\KBA_$($service.Name)" `
        -ApplicationPool "KBA_$($service.Name)Pool" `
        -Port $($service.Port)
    
    # Permissions
    $acl = Get-Acl "C:\inetpub\wwwroot\KBA_$($service.Name)"
    $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
        "IIS AppPool\KBA_$($service.Name)Pool",
        "Modify",
        "ContainerInherit,ObjectInherit",
        "None",
        "Allow"
    )
    $acl.SetAccessRule($rule)
    Set-Acl "C:\inetpub\wwwroot\KBA_$($service.Name)" $acl
}

Write-Host "✅ Déploiement IIS terminé!" -ForegroundColor Green
```

#### web.config (pour chaque service)

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath="dotnet"
                arguments=".\KBA.IdentityService.dll"
                stdoutLogEnabled="true"
                stdoutLogFile=".\logs\stdout"
                hostingModel="inprocess" />
  </system.webServer>
</configuration>
```

### Déploiement Hybride (Recommandé)

**Scénario** : Gateway sur IIS, Services sur Docker

```
IIS (Port 80)
  ├─ API Gateway → Reverse Proxy
  │
  └─ Docker Containers
      ├─ Identity Service (Container)
      ├─ Product Service (Container)
      └─ Tenant Service (Container)
```

**Configuration Gateway** :
```json
{
  "Routes": [
    {
      "DownstreamHostAndPorts": [
        {
          "Host": "localhost",  // ou IP du host Docker
          "Port": 5001
        }
      ]
    }
  ]
}
```

## 7️⃣ Logging

### Configuration Serilog (Déjà implémentée)

Chaque service log dans son propre fichier :
- `logs/identity-service-YYYYMMDD.log`
- `logs/product-service-YYYYMMDD.log`
- `logs/tenant-service-YYYYMMDD.log`
- `logs/api-gateway-YYYYMMDD.log`

### Centralisation des Logs

#### Option A : Seq (Recommandé)

**Installation** :
```powershell
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest
```

**Configuration** dans chaque service :
```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/service-.log", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq("http://localhost:5341")  // Seq
    .Enrich.WithProperty("Service", "IdentityService")
    .CreateLogger();
```

**Package** :
```xml
<PackageReference Include="Serilog.Sinks.Seq" Version="7.0.0" />
```

**Accès** : http://localhost:5341

#### Option B : ELK Stack (Production)

**docker-compose.yml** :
```yaml
services:
  elasticsearch:
    image: elasticsearch:8.11.0
    ports:
      - "9200:9200"
  
  logstash:
    image: logstash:8.11.0
    ports:
      - "5044:5044"
  
  kibana:
    image: kibana:8.11.0
    ports:
      - "5601:5601"
```

**Configuration** :
```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
    {
        AutoRegisterTemplate = true,
        IndexFormat = "kba-logs-{0:yyyy.MM}"
    })
    .CreateLogger();
```

### Correlation ID (Tracing)

**Middleware** partagé :
```csharp
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
```

**Utilisation** :
```csharp
app.UseMiddleware<CorrelationIdMiddleware>();
```

### Structured Logging

```csharp
// ❌ Mauvais
_logger.LogInformation($"User {userId} created product {productId}");

// ✅ Bon
_logger.LogInformation(
    "User {UserId} created product {ProductId}",
    userId,
    productId
);
```

### Log Levels

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "KBA": "Debug"
      }
    }
  }
}
```

---

## 📋 Checklist : Passage à la Base Unique

- [ ] Modifier tous les `appsettings.json` pour pointer vers `KBAFrameworkDb`
- [ ] Supprimer les bases séparées : `KBAIdentityDb`, `KBAProductDb`, `KBATenantDb`
- [ ] Exécuter les migrations dans l'ordre
- [ ] Tester la communication entre services
- [ ] Vérifier les logs centralisés

## 🎯 Résumé

1. **Base unique** : Plus simple, utilisez `KBAFrameworkDb` partout
2. **Communication** : HTTP synchrone via Gateway, RabbitMQ asynchrone
3. **Nouveau service** : Suivez le template (Program.cs, DbContext, Controllers)
4. **Dossier src/** : Code partagé (Domain obligatoire)
5. **Débogage** : Multi-startup dans Visual Studio
6. **Déploiement** : Docker ou IIS, ou hybride
7. **Logging** : Serilog + Seq pour centralisation

