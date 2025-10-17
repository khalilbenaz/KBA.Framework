# 📋 Réponses à Vos Questions sur les Microservices

## 1️⃣ Base de Données Unique ✅

### Configuration Actuelle

**Tous les microservices utilisent maintenant la même base de données : `KBAFrameworkDb`**

Les fichiers `appsettings.json` de tous les services ont été mis à jour :

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=KBAFrameworkDb;Trusted_Connection=true;TrustServerCertificate=true"
  }
}
```

### Structure de la Base Unique

```
KBAFrameworkDb
├── KBA.Users               ← Identity Service
├── KBA.Roles               ← Identity Service
├── KBA.Products            ← Product Service
├── KBA.Tenants             ← Tenant Service
├── KBA.Orders              ← Order Service (si créé)
└── KBA.OrderItems          ← Order Service (si créé)
```

### Avantages

✅ **Simplicité** : Une seule base à gérer  
✅ **Pas de cohérence éventuelle** : Toutes les données sont immédiatement cohérentes  
✅ **Transactions** : Possibilité de transactions entre tables  
✅ **Migrations** : Plus faciles à gérer  
✅ **Débogage** : Plus simple à déboguer

### Comment Démarrer

```powershell
# 1. Supprimer les anciennes bases (si elles existent)
dotnet ef database drop --project microservices\KBA.IdentityService
dotnet ef database drop --project microservices\KBA.ProductService
dotnet ef database drop --project microservices\KBA.TenantService

# 2. Créer la base unique avec toutes les migrations
cd microservices\KBA.IdentityService
dotnet ef database update

cd ..\KBA.ProductService
dotnet ef database update

cd ..\KBA.TenantService
dotnet ef database update

# 3. Démarrer les services
cd ..
.\start-microservices.ps1
```

---

## 2️⃣ Communication entre Microservices

### A. Communication Synchrone (HTTP/REST)

#### Via l'API Gateway (Recommandé)

```
Service A → API Gateway → Service B
```

**Exemple** : Product Service appelle Identity Service

```csharp
// Dans KBA.ProductService/Program.cs
builder.Services.AddHttpClient("IdentityService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5001");
    // Ou via le gateway : new Uri("http://localhost:5000/api/identity");
});

// Dans un service
public class ProductServiceLogic
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    public async Task<bool> ValidateUserAsync(Guid userId)
    {
        var client = _httpClientFactory.CreateClient("IdentityService");
        
        var response = await client.GetAsync($"/api/users/{userId}");
        return response.IsSuccessStatusCode;
    }
}
```

#### Appel Direct (Non recommandé mais possible)

```csharp
// Appel direct sans passer par le gateway
var client = new HttpClient();
var response = await client.GetAsync("http://localhost:5001/api/users/123");
```

### B. Communication Asynchrone (Message Queue)

#### Avec RabbitMQ + MassTransit

**1. Installation de RabbitMQ**

```powershell
docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3-management
```

**2. Packages NuGet** (à ajouter dans chaque service)

```xml
<PackageReference Include="MassTransit" Version="8.1.3" />
<PackageReference Include="MassTransit.RabbitMQ" Version="8.1.3" />
```

**3. Définir les événements** (dans `src/KBA.Framework.Domain/Events/`)

```csharp
// UserCreatedEvent.cs
namespace KBA.Framework.Domain.Events;

public record UserCreatedEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public Guid? TenantId { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record ProductCreatedEvent
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
}
```

**4. Publier un événement** (Identity Service)

```csharp
// Dans IdentityService/Services/UserService.cs
using MassTransit;

public class UserService
{
    private readonly IPublishEndpoint _publishEndpoint;
    
    public async Task<UserDto> CreateAsync(CreateUserDto dto)
    {
        var user = new User(...);
        await _context.SaveChangesAsync();
        
        // Publier l'événement
        await _publishEndpoint.Publish(new UserCreatedEvent
        {
            UserId = user.Id,
            Email = user.Email,
            TenantId = user.TenantId,
            CreatedAt = DateTime.UtcNow
        });
        
        return MapToDto(user);
    }
}

// Dans Program.cs
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
        cfg.ConfigureEndpoints(context);
    });
});
```

**5. Consommer un événement** (Product Service)

```csharp
// Dans ProductService/Consumers/UserCreatedConsumer.cs
using MassTransit;

public class UserCreatedConsumer : IConsumer<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedConsumer> _logger;
    
    public async Task Consume(ConsumeContext<UserCreatedEvent> context)
    {
        var evt = context.Message;
        
        _logger.LogInformation(
            "User created: {UserId} - {Email}", 
            evt.UserId, 
            evt.Email
        );
        
        // Faire quelque chose avec l'événement
        // Par exemple : créer un catalogue par défaut pour l'utilisateur
    }
}

// Dans Program.cs
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<UserCreatedConsumer>();
    
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
        cfg.ConfigureEndpoints(context);
    });
});
```

### Résumé Communication

| Type | Quand l'utiliser | Avantages | Inconvénients |
|------|------------------|-----------|---------------|
| **Synchrone HTTP** | Besoin d'une réponse immédiate | Simple, direct | Couplage, latence |
| **Asynchrone MQ** | Événements, notifications | Découplage, résilience | Complexité, cohérence éventuelle |

---

## 3️⃣ Ajouter un Nouveau Microservice

### Guide Complet

**Voir le guide détaillé** : [`docs/ADD-NEW-SERVICE.md`](./docs/ADD-NEW-SERVICE.md)

### Résumé en 10 Étapes

1. **Créer le projet** : `dotnet new webapi -n KBA.NouveauService`
2. **Ajouter les entités** dans `src/KBA.Framework.Domain/Entities/`
3. **Créer le DbContext** avec les configurations EF Core
4. **Créer les DTOs** (Data Transfer Objects)
5. **Créer le Service Logic** (logique métier)
6. **Créer les Controllers** (endpoints API)
7. **Créer Program.cs** avec JWT, Serilog, DbContext
8. **Créer appsettings.json** avec la connexion unique
9. **Ajouter la route** dans `KBA.ApiGateway/ocelot.json`
10. **Tester** le nouveau service

### Template Rapide

```powershell
# Script pour créer un nouveau service
$serviceName = "Order"  # Changez ceci

cd microservices
mkdir KBA.$($serviceName)Service
cd KBA.$($serviceName)Service

# Créer la structure
mkdir Controllers, Services, DTOs, Data

# Le reste suit le pattern des services existants
```

---

## 4️⃣ Utilisation du Dossier `src/`

### Structure Actuelle

```
src/
├── KBA.Framework.Domain/           ← ✅ UTILISÉ par tous les microservices
│   ├── Entities/                   ← Entités partagées
│   ├── Events/                     ← Événements de domaine
│   ├── Repositories/               ← Interfaces
│   └── Common/                     ← Classes de base
│
├── KBA.Framework.Application/      ← ⚠️ Non utilisé dans microservices
├── KBA.Framework.Infrastructure/   ← ⚠️ Non utilisé dans microservices
└── KBA.Framework.Api/              ← ℹ️ Ancien monolithe
```

### Ce qui est Utilisé

#### ✅ `src/KBA.Framework.Domain/` - PARTAGÉ

**Tous les microservices** référencent ce projet :

```xml
<!-- Dans chaque microservice .csproj -->
<ItemGroup>
  <ProjectReference Include="..\..\src\KBA.Framework.Domain\KBA.Framework.Domain.csproj" />
</ItemGroup>
```

**Contient** :
- **Entités** : `User`, `Product`, `Tenant`, `Order`, etc.
- **Value Objects** : `Email`, `Money`, etc.
- **Événements** : `UserCreatedEvent`, `ProductCreatedEvent`
- **Interfaces** : `IRepository<T>`, `IAggregateRoot`
- **Constantes** : `KBAConsts.TablePrefix`

**Pourquoi ?**
- Éviter la duplication du code de domaine
- Assurer la cohérence des règles métier
- Faciliter la communication inter-services

### Ce qui n'est PAS Utilisé

#### ❌ `src/KBA.Framework.Application/`

**Raison** : Chaque microservice a sa propre couche application (Services, DTOs)

**Alternative** : Créer un package NuGet `KBA.Framework.Shared` avec :
- Validators communs
- DTOs de base
- Extensions utiles

#### ❌ `src/KBA.Framework.Infrastructure/`

**Raison** : Chaque microservice gère sa propre infrastructure

**Ce qui se passe** : Chaque service a son propre :
- `DbContext`
- Repositories (si nécessaire)
- Services d'infrastructure

#### ℹ️ `src/KBA.Framework.Api/` - Ancien Monolithe

**Options** :
1. **Garder** pour la compatibilité avec les anciens clients
2. **Migrer progressivement** vers les microservices
3. **Supprimer** une fois la migration terminée

**Actuellement** : Coexiste avec les microservices (Strangler Fig Pattern)

### Recommandation

```
src/
├── KBA.Framework.Domain/          ← GARDER (partagé)
├── KBA.Framework.Shared/          ← CRÉER (utilitaires partagés)
└── KBA.Framework.Api/             ← OPTIONNEL (ancien monolithe)
```

---

## 5️⃣ Débogage avec Microservices

### Méthode 1 : Visual Studio Multi-Startup (⭐ Recommandé)

**Configuration** :

1. Clic droit sur la solution `KBA.Microservices.sln`
2. **Configure Startup Projects**
3. Sélectionner **Multiple startup projects**
4. Configurer :
   - KBA.ApiGateway → **Start**
   - KBA.IdentityService → **Start**
   - KBA.ProductService → **Start**
   - KBA.TenantService → **Start**

5. **Appuyer sur F5** → Tous démarrent avec le debugger !

**Avantages** :
- ✅ Breakpoints dans tous les services simultanément
- ✅ Pas besoin de Docker
- ✅ Hot Reload disponible
- ✅ Debugging temps réel

### Méthode 2 : Visual Studio Code

**Créer `.vscode/launch.json`** :

```json
{
  "version": "0.2.0",
  "compounds": [
    {
      "name": "All Microservices",
      "configurations": [
        "API Gateway",
        "Identity Service",
        "Product Service",
        "Tenant Service"
      ],
      "stopAll": true
    }
  ]
}
```

**Voir le guide complet** : [`docs/DEBUGGING-GUIDE.md`](./docs/DEBUGGING-GUIDE.md)

### Méthode 3 : Attach to Process

```powershell
# 1. Démarrer normalement
.\start-microservices.ps1

# 2. Dans Visual Studio : Debug → Attach to Process (Ctrl+Alt+P)
# 3. Filtrer par "dotnet.exe"
# 4. Sélectionner les processus des microservices
```

### Debugging Docker

```dockerfile
# Dockerfile.debug avec vsdbg
RUN curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg
```

### Outils Complémentaires

- **Seq** : Logs centralisés → http://localhost:5341
- **Postman/Swagger** : Tester les endpoints
- **SQL Profiler** : Tracer les requêtes
- **Fiddler** : Capturer le trafic HTTP

---

## 6️⃣ Déploiement Docker ET IIS

### Option 1 : Docker Compose (Tout en Docker)

```powershell
cd microservices
docker-compose up -d
```

**Avantages** :
- ✅ Isolation complète
- ✅ Portable (dev, staging, prod)
- ✅ Facilement scalable

**Accès** :
- API Gateway : http://localhost:5000
- Identity : http://localhost:5001
- Product : http://localhost:5002
- Tenant : http://localhost:5003

### Option 2 : IIS (Tout sur IIS)

```powershell
# Exécuter en tant qu'Administrateur
.\deploy-microservices-iis.ps1
```

**Ce que fait le script** :
1. Publie chaque service (Release)
2. Crée un pool d'application par service
3. Crée un site IIS par service
4. Configure les permissions
5. Démarre tous les services

**Accès** :
- Mêmes URLs que Docker : http://localhost:5000-5003

### Option 3 : Hybride (Gateway IIS + Services Docker) ⭐

**Scénario idéal pour la production** :

```
Internet → IIS (Port 80/443)
            ↓
         API Gateway (IIS)
            ↓
    ┌──────┴──────┬──────────┐
    ↓             ↓          ↓
Identity (Docker) Product   Tenant
                (Docker)   (Docker)
```

**Configuration Gateway IIS** :

```json
// ocelot.json - pointer vers les containers
{
  "DownstreamHostAndPorts": [
    {
      "Host": "localhost",  // ou IP Docker host
      "Port": 5001
    }
  ]
}
```

**Avantages** :
- ✅ Gateway stable sur IIS
- ✅ Services scalables en Docker
- ✅ Meilleur des deux mondes

### Comparaison

| Critère | Docker | IIS | Hybride |
|---------|--------|-----|---------|
| **Simplicité** | ⭐⭐⭐ | ⭐⭐ | ⭐⭐ |
| **Scalabilité** | ⭐⭐⭐ | ⭐ | ⭐⭐⭐ |
| **Performance** | ⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ |
| **Windows natif** | ⭐ | ⭐⭐⭐ | ⭐⭐⭐ |
| **Production** | ⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐ |

---

## 7️⃣ Logging Complet

### Configuration Actuelle

**Chaque service** log avec Serilog dans :
- Console
- Fichiers (`logs/service-YYYYMMDD.log`)
- Seq (si disponible)

### Logging Centralisé avec Seq

**1. Démarrer Seq**

```powershell
docker run -d --name seq -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest
```

**2. Configuration déjà dans les services** :

```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/service-.log", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq("http://localhost:5341")  // ← Seq
    .Enrich.WithProperty("Service", "IdentityService")
    .Enrich.WithProperty("Environment", "Development")
    .CreateLogger();
```

**3. Accéder à Seq** : http://localhost:5341

**Recherches dans Seq** :
```
Service = 'IdentityService' and Level = 'Error'
CorrelationId = 'abc-123'
@Message like '%user%'
```

### Correlation ID (Traçabilité)

**Créer un middleware** (à ajouter dans chaque service) :

```csharp
// Middleware/CorrelationIdMiddleware.cs
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";
    
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        
        context.Response.Headers[CorrelationIdHeader] = correlationId;
        
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}

// Dans Program.cs
app.UseMiddleware<CorrelationIdMiddleware>();
```

**Utilisation** :

```
Requête 1 → Gateway → Identity Service → Product Service
   ↓           ↓              ↓                  ↓
X-Correlation-ID: abc-123-... (même ID partout)
```

**Recherche dans Seq** :
```
CorrelationId = 'abc-123-...'
```
→ Voir TOUS les logs de cette requête dans tous les services !

### Structured Logging

```csharp
// ❌ Mauvais
_logger.LogInformation($"User {userId} created product {productId}");

// ✅ Bon (structured)
_logger.LogInformation(
    "User {UserId} created product {ProductId} with price {Price}",
    userId,
    productId,
    price
);
```

**Avantage** : Filtrable dans Seq :
```
UserId = '123-456-...'
Price > 1000
```

### Log Levels

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Information",
        "KBA": "Debug"
      }
    }
  }
}
```

### Fichiers de Log

**Localisation** :
```
microservices/
├── KBA.IdentityService/logs/
│   └── identity-service-20251016.log
├── KBA.ProductService/logs/
│   └── product-service-20251016.log
├── KBA.TenantService/logs/
│   └── tenant-service-20251016.log
└── KBA.ApiGateway/logs/
    └── api-gateway-20251016.log
```

**Consulter les logs** :

```powershell
# Dernières 50 lignes d'Identity Service
Get-Content "microservices\KBA.IdentityService\logs\identity-service-*.log" -Tail 50

# Rechercher une erreur
Get-Content "microservices\*\logs\*.log" | Select-String "Exception"

# Suivre en temps réel
Get-Content "microservices\KBA.ApiGateway\logs\*.log" -Wait -Tail 20
```

### ELK Stack (Production)

Pour la production, utilisez **Elasticsearch + Logstash + Kibana** :

```yaml
# docker-compose.logging.yml
services:
  elasticsearch:
    image: elasticsearch:8.11.0
    ports:
      - "9200:9200"
  
  kibana:
    image: kibana:8.11.0
    ports:
      - "5601:5601"
```

---

## 📚 Documentation Complète

Tous les guides détaillés :

1. **[FAQ-COMPLETE.md](./docs/FAQ-COMPLETE.md)** - FAQ exhaustive
2. **[ADD-NEW-SERVICE.md](./docs/ADD-NEW-SERVICE.md)** - Guide complet pour ajouter un service
3. **[DEBUGGING-GUIDE.md](./docs/DEBUGGING-GUIDE.md)** - Toutes les techniques de debugging
4. **[ARCHITECTURE.md](./docs/ARCHITECTURE.md)** - Architecture détaillée
5. **[QUICKSTART.md](./QUICKSTART.md)** - Démarrage rapide

---

## ✅ Résumé des Réponses

| Question | Réponse | Guide |
|----------|---------|-------|
| **Base unique ?** | ✅ Oui, `KBAFrameworkDb` configurée | Configuration modifiée |
| **Communication ?** | HTTP synchrone + RabbitMQ asynchrone | FAQ-COMPLETE.md |
| **Nouveau service ?** | Template en 10 étapes | ADD-NEW-SERVICE.md |
| **Dossier src/ ?** | Domain partagé, reste non utilisé | Section 4 ci-dessus |
| **Débogage ?** | Multi-startup VS, Attach, Docker | DEBUGGING-GUIDE.md |
| **Docker + IIS ?** | Les deux possibles, même hybride | Section 6 ci-dessus |
| **Logging ?** | Serilog + Seq + Correlation ID | Section 7 ci-dessus |

---

## 🚀 Prochaines Étapes

1. **Démarrer les services** : `.\start-microservices.ps1`
2. **Accéder au Gateway** : http://localhost:5000
3. **Consulter Swagger** : http://localhost:5000/swagger
4. **Lancer Seq** : `docker run -d -e ACCEPT_EULA=Y -p 5341:80 datalust/seq`
5. **Tester la communication** entre services
6. **Créer votre premier nouveau service** (ex: Order)
7. **Déployer sur Docker** : `docker-compose up -d`
8. **Ou déployer sur IIS** : `.\deploy-microservices-iis.ps1`

**Vous avez maintenant une architecture microservices complète, documentée et prête pour la production ! 🎉**
