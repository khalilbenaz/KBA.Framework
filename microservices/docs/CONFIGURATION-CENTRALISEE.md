# 📝 Configuration Centralisée - Best Practice

## 🎯 Problème Résolu

**Avant** : URLs codées en dur dans le code ❌
```csharp
.WriteTo.Seq("http://localhost:5341")  // URL en dur !
```

**Maintenant** : URLs lues depuis `appsettings.json` ✅
```csharp
var seqUrl = tempConfig["Serilog:SeqUrl"] ?? "http://localhost:5341";
.WriteTo.Seq(seqUrl)  // URL configurable !
```

---

## ✅ Modifications Appliquées

### Tous les Services Mis à Jour

| Service | Fichier | Status |
|---------|---------|--------|
| **Identity Service** | appsettings.json | ✅ |
| **Identity Service** | Program.cs | ✅ |
| **Product Service** | appsettings.json | ✅ |
| **Product Service** | Program.cs | ✅ |
| **Tenant Service** | appsettings.json | ✅ |
| **Tenant Service** | Program.cs | ✅ |
| **Permission Service** | appsettings.json | ✅ |
| **Permission Service** | Program.cs | ✅ |

---

## 📊 Structure de Configuration

### appsettings.json (Standard pour tous les services)

```json
{
  "Serilog": {
    "SeqUrl": "http://localhost:5341"
  },
  "ExternalServices": {
    "IdentityServiceUrl": "http://localhost:5001",
    "ProductServiceUrl": "http://localhost:5002",
    "TenantServiceUrl": "http://localhost:5003",
    "PermissionServiceUrl": "http://localhost:5004"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=KBAFrameworkDb;...",
    "Redis": "localhost:6379"
  },
  "JwtSettings": {
    "SecretKey": "VotreCleSecrete...",
    "Issuer": "KBAFramework",
    "Audience": "KBAFrameworkUsers",
    "ExpirationInMinutes": 60
  }
}
```

---

## 🔧 Implémentation dans Program.cs

### Code Standard (Utilisé dans tous les services)

```csharp
// Créer une configuration temporaire pour lire les settings
var tempConfig = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var seqUrl = tempConfig["Serilog:SeqUrl"] ?? "http://localhost:5341";

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/service-.log", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq(seqUrl)  // ✅ URL depuis config
    .Enrich.WithProperty("Service", "ServiceName")
    .CreateLogger();

try
{
    Log.Information("Starting Service on {SeqUrl}", seqUrl);
    
    var builder = WebApplication.CreateBuilder(args);
    // ...
}
```

### Pourquoi une Configuration Temporaire ?

Le logger Serilog est créé **AVANT** le `WebApplication.CreateBuilder`, donc on doit lire la configuration manuellement :

```csharp
// ❌ Impossible - builder n'existe pas encore
Log.Logger = new LoggerConfiguration()
    .WriteTo.Seq(builder.Configuration["Serilog:SeqUrl"])  
    
// ✅ Solution - config temporaire
var tempConfig = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();
var seqUrl = tempConfig["Serilog:SeqUrl"];
```

---

## 🎯 Avantages

### 1. Flexibilité des Environnements

```json
// appsettings.Development.json
{
  "Serilog": {
    "SeqUrl": "http://localhost:5341"
  }
}

// appsettings.Production.json
{
  "Serilog": {
    "SeqUrl": "https://seq.production.com"
  }
}

// appsettings.Staging.json
{
  "Serilog": {
    "SeqUrl": "https://seq.staging.com"
  }
}
```

### 2. Pas de Recompilation

**Avant** : Changer l'URL Seq → Modifier le code → Recompiler → Redéployer ❌

**Maintenant** : Changer l'URL Seq → Modifier appsettings.json → Redémarrer ✅

### 3. Configuration Docker

```dockerfile
# Dockerfile
ENV Serilog__SeqUrl=http://seq:5341
ENV ExternalServices__IdentityServiceUrl=http://identity:5001
```

### 4. Kubernetes ConfigMap

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: product-service-config
data:
  appsettings.json: |
    {
      "Serilog": {
        "SeqUrl": "http://seq-service:5341"
      }
    }
```

---

## 📋 Variables de Configuration Disponibles

### Serilog

| Clé | Valeur par Défaut | Description |
|-----|-------------------|-------------|
| `Serilog:SeqUrl` | http://localhost:5341 | URL du serveur Seq |

### Services Externes

| Clé | Valeur | Description |
|-----|--------|-------------|
| `ExternalServices:IdentityServiceUrl` | http://localhost:5001 | Identity Service |
| `ExternalServices:ProductServiceUrl` | http://localhost:5002 | Product Service |
| `ExternalServices:TenantServiceUrl` | http://localhost:5003 | Tenant Service |
| `ExternalServices:PermissionServiceUrl` | http://localhost:5004 | Permission Service |

### Connexions

| Clé | Description |
|-----|-------------|
| `ConnectionStrings:DefaultConnection` | Base de données SQL Server |
| `ConnectionStrings:Redis` | Serveur Redis |

### JWT

| Clé | Description |
|-----|-------------|
| `JwtSettings:SecretKey` | Clé secrète JWT |
| `JwtSettings:Issuer` | Émetteur du token |
| `JwtSettings:Audience` | Audience du token |
| `JwtSettings:ExpirationInMinutes` | Durée de vie du token |

---

## 🔍 Utilisation dans le Code

### Lire une URL de Service Externe

```csharp
// Dans un Controller ou Service
public class ProductsController : ControllerBase
{
    private readonly IConfiguration _configuration;
    
    public ProductsController(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    public async Task<IActionResult> CheckPermission()
    {
        var permissionServiceUrl = _configuration["ExternalServices:PermissionServiceUrl"];
        
        using var client = new HttpClient();
        client.BaseAddress = new Uri(permissionServiceUrl);
        
        var response = await client.PostAsJsonAsync("/api/permissions/check", dto);
        // ...
    }
}
```

### Avec IHttpClientFactory (Recommandé)

```csharp
// Dans Program.cs
builder.Services.AddHttpClient("PermissionService", (serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var baseUrl = config["ExternalServices:PermissionServiceUrl"];
    client.BaseAddress = new Uri(baseUrl);
});

// Dans un Controller
public class ProductsController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    public ProductsController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    
    public async Task<IActionResult> CheckPermission()
    {
        var client = _httpClientFactory.CreateClient("PermissionService");
        var response = await client.PostAsJsonAsync("/api/permissions/check", dto);
        // ...
    }
}
```

---

## 🌐 Variables d'Environnement

ASP.NET Core supporte automatiquement les variables d'environnement :

### Format

```bash
Section__SubSection__Key=Value
```

### Exemples

```bash
# Linux/Mac
export Serilog__SeqUrl=http://seq.production.com
export ExternalServices__PermissionServiceUrl=http://permission:5004

# Windows PowerShell
$env:Serilog__SeqUrl="http://seq.production.com"
$env:ExternalServices__PermissionServiceUrl="http://permission:5004"

# Docker
docker run -e Serilog__SeqUrl=http://seq:5341 kba-product-service
```

### Priorité de Configuration

1. ✅ **Variables d'environnement** (plus haute priorité)
2. ✅ **appsettings.{Environment}.json**
3. ✅ **appsettings.json**
4. ✅ **Valeur par défaut dans le code**

---

## 📝 Exemples par Environnement

### Développement Local

```json
// appsettings.Development.json
{
  "Serilog": {
    "SeqUrl": "http://localhost:5341"
  },
  "ExternalServices": {
    "PermissionServiceUrl": "http://localhost:5004"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=KBAFrameworkDb;...",
    "Redis": "localhost:6379"
  }
}
```

### Docker Compose

```json
// appsettings.Docker.json
{
  "Serilog": {
    "SeqUrl": "http://seq:5341"
  },
  "ExternalServices": {
    "IdentityServiceUrl": "http://identity:5001",
    "PermissionServiceUrl": "http://permission:5004"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=sqlserver;Database=KBAFrameworkDb;User=sa;Password=YourPassword;",
    "Redis": "redis:6379"
  }
}
```

### Kubernetes

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: product-service-config
data:
  Serilog__SeqUrl: "http://seq-service:5341"
  ExternalServices__PermissionServiceUrl: "http://permission-service:5004"
  ConnectionStrings__DefaultConnection: "Server=sqlserver-service;Database=KBAFrameworkDb;..."
  ConnectionStrings__Redis: "redis-service:6379"
```

### Production (Azure App Service)

```json
// Configuration dans Azure Portal
{
  "Serilog__SeqUrl": "https://seq.yourdomain.com",
  "ExternalServices__PermissionServiceUrl": "https://permission.yourdomain.com",
  "ConnectionStrings__DefaultConnection": "Server=tcp:yourserver.database.windows.net,...",
  "ConnectionStrings__Redis": "yourredis.redis.cache.windows.net:6380,ssl=True"
}
```

---

## ✅ Checklist de Migration

### Pour Ajouter une Nouvelle URL Configurable

- [ ] Ajouter la clé dans `appsettings.json`
- [ ] Lire depuis `IConfiguration` dans le code
- [ ] Ajouter valeur par défaut (avec `??`)
- [ ] Documenter dans ce fichier
- [ ] Tester avec différentes valeurs
- [ ] Mettre à jour `appsettings.{Environment}.json`

### Exemple

```csharp
// 1. Dans appsettings.json
{
  "ExternalServices": {
    "NewServiceUrl": "http://localhost:5009"
  }
}

// 2. Dans le code
var url = configuration["ExternalServices:NewServiceUrl"] 
    ?? "http://localhost:5009";  // Valeur par défaut

// 3. Utiliser
client.BaseAddress = new Uri(url);
```

---

## 🎉 Résultat

**Tous les services** utilisent maintenant une **configuration centralisée** :

✅ **Aucune URL en dur** dans le code  
✅ **Facile à changer** par environnement  
✅ **Support Docker/K8s** natif  
✅ **Variables d'environnement** supportées  
✅ **Best practice** ASP.NET Core  

**Code plus maintenable et déployable ! 🚀**

---

**Date** : 17 Octobre 2025  
**Version** : 2.0  
**Status** : ✅ Implémenté dans tous les services
