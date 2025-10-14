# KBA Framework - Clean Architecture .NET 8

![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![License](https://img.shields.io/badge/license-MIT-green)
![Build](https://img.shields.io/badge/build-passing-brightgreen)

Framework d'entreprise complet basé sur Clean Architecture, DDD, et multi-tenancy pour applications SaaS professionnelles.

## 📋 Table des Matières

- [Vue d'ensemble](#-vue-densemble)
- [Architecture](#-architecture)
- [Structure du projet](#-structure-du-projet)
- [Tables de la base de données](#-tables-de-la-base-de-données)
- [Guide de démarrage rapide](#-guide-de-démarrage-rapide)
- [Guide complet](#-guide-complet)
- [Déploiement IIS](#-déploiement-iis)
- [Tests](#-tests)
- [Troubleshooting](#-troubleshooting)
- [Checklist de production](#-checklist-de-production)

## 🎯 Vue d'ensemble

KBA Framework est un framework d'entreprise production-ready qui implémente les meilleures pratiques de développement .NET:

### Caractéristiques principales

✅ **Clean Architecture** - Séparation stricte des responsabilités  
✅ **Domain-Driven Design (DDD)** - Modélisation riche du domaine métier  
✅ **Multi-Tenancy complet** - Support SaaS natif avec isolation des données  
✅ **Audit Logging automatique** - Traçabilité complète de toutes les opérations  
✅ **Repository Pattern** - Abstraction de la couche de données  
✅ **Entity Framework Core 8** - ORM moderne et performant  
✅ **Tests complets** - Unit tests, integration tests avec xUnit  
✅ **Swagger/OpenAPI** - Documentation interactive de l'API  
✅ **Déploiement IIS automatisé** - Script PowerShell clé en main  

### Technologies utilisées

- **.NET 8** - Framework principal
- **Entity Framework Core 8** - ORM
- **SQL Server** - Base de données (compatible LocalDB)
- **xUnit** - Framework de tests
- **Moq** - Bibliothèque de mocking
- **Swagger/Swashbuckle** - Documentation API
- **C# 12** - Dernières fonctionnalités du langage

## 🏗️ Architecture

### Principes Clean Architecture

```
┌─────────────────────────────────────────────────────┐
│                   Presentation                       │
│              (KBA.Framework.Api)                     │
│         Controllers, DTOs, Configuration             │
└────────────────────┬────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────┐
│                  Application                         │
│          (KBA.Framework.Application)                 │
│         Services, DTOs, Interfaces                   │
└────────────────────┬────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────┐
│                Infrastructure                        │
│         (KBA.Framework.Infrastructure)               │
│    DbContext, Repositories, Configurations           │
└────────────────────┬────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────┐
│                    Domain                            │
│             (KBA.Framework.Domain)                   │
│    Entities, Value Objects, Domain Events            │
└─────────────────────────────────────────────────────┘
```

### Dépendances

- **Domain** → Aucune dépendance (centre de l'architecture)
- **Application** → Domain
- **Infrastructure** → Domain + Application
- **API** → Application + Infrastructure

## 📁 Structure du projet

```
KBA.Framework/
├── src/
│   ├── KBA.Framework.Domain/          # Couche domaine
│   │   ├── Entities/                  # Entités métier
│   │   │   ├── Identity/              # Utilisateurs, Rôles
│   │   │   ├── MultiTenancy/          # Tenants
│   │   │   ├── Products/              # Produits (exemple)
│   │   │   ├── Permissions/           # Permissions
│   │   │   ├── Auditing/              # Audit logs
│   │   │   ├── Organization/          # Unités organisationnelles
│   │   │   ├── Configuration/         # Settings, Features
│   │   │   └── BackgroundJobs/        # Tâches asynchrones
│   │   ├── Events/                    # Événements de domaine
│   │   ├── Repositories/              # Interfaces des repositories
│   │   └── KBAConsts.cs               # Constantes globales
│   │
│   ├── KBA.Framework.Application/     # Couche application
│   │   ├── Services/                  # Services applicatifs
│   │   │   ├── IProductService.cs
│   │   │   ├── ProductService.cs
│   │   │   ├── IUserService.cs
│   │   │   └── UserService.cs
│   │   └── DTOs/                      # Data Transfer Objects
│   │       ├── Products/
│   │       ├── Users/
│   │       └── Tenants/
│   │
│   ├── KBA.Framework.Infrastructure/  # Couche infrastructure
│   │   ├── Data/
│   │   │   ├── KBADbContext.cs        # DbContext principal
│   │   │   ├── Configurations/        # Configurations EF Core
│   │   │   └── Migrations/            # Migrations de la BD
│   │   └── Repositories/              # Implémentations des repositories
│   │       ├── Repository.cs          # Repository générique
│   │       ├── ProductRepository.cs
│   │       └── UserRepository.cs
│   │
│   └── KBA.Framework.Api/             # Couche présentation
│       ├── Controllers/               # Contrôleurs API
│       │   ├── ProductsController.cs
│       │   └── UsersController.cs
│       ├── Program.cs                 # Point d'entrée
│       ├── appsettings.json           # Configuration
│       └── web.config                 # Configuration IIS
│
├── tests/
│   ├── KBA.Framework.Domain.Tests/         # Tests du domaine
│   ├── KBA.Framework.Application.Tests/    # Tests de l'application
│   └── KBA.Framework.Api.IntegrationTests/ # Tests d'intégration
│
├── deploy-iis.ps1                     # Script de déploiement IIS
├── README.md                          # Ce fichier
├── GUIDE-COMPLET.md                   # Documentation approfondie
└── KBA.Framework.sln                  # Solution Visual Studio
```

## 🗄️ Tables de la base de données

Toutes les tables utilisent le préfixe **KBA.**

### Identity Management
- **KBA.Users** - Utilisateurs du système
- **KBA.Roles** - Rôles et permissions
- **KBA.UserRoles** - Association utilisateurs-rôles
- **KBA.UserClaims** - Claims des utilisateurs
- **KBA.RoleClaims** - Claims des rôles
- **KBA.UserLogins** - Logins externes (OAuth, etc.)
- **KBA.UserTokens** - Tokens d'authentification

### Multi-Tenancy
- **KBA.Tenants** - Tenants (organisations/clients)
- **KBA.TenantConnectionStrings** - Chaînes de connexion par tenant

### Permissions
- **KBA.Permissions** - Définitions des permissions
- **KBA.PermissionGrants** - Attributions de permissions

### Audit Logging
- **KBA.AuditLogs** - Journaux d'audit des opérations
- **KBA.AuditLogActions** - Actions spécifiques dans les logs
- **KBA.EntityChanges** - Changements sur les entités
- **KBA.EntityPropertyChanges** - Détails des modifications de propriétés

### Configuration
- **KBA.Settings** - Paramètres de configuration
- **KBA.FeatureValues** - Valeurs des fonctionnalités

### Organization
- **KBA.OrganizationUnits** - Unités organisationnelles
- **KBA.UserOrganizationUnits** - Affectation des utilisateurs

### Système
- **KBA.BackgroundJobs** - Gestion des tâches en arrière-plan

### Business (Exemple)
- **KBA.Products** - Produits (exemple d'entité métier)

## 🚀 Guide de démarrage rapide

### Prérequis

- .NET 8 SDK ([télécharger](https://dotnet.microsoft.com/download/dotnet/8.0))
- SQL Server ou SQL Server LocalDB
- Visual Studio 2022+ ou VS Code (optionnel)
- Git (optionnel)

### Installation en 5 minutes

#### 1. Cloner ou télécharger le projet

```bash
git clone https://github.com/votre-repo/KBA.Framework.git
cd KBA.Framework
```

#### 2. Restaurer les packages NuGet

```bash
dotnet restore
```

#### 3. Configurer la chaîne de connexion

Éditer `src/KBA.Framework.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=KBAFrameworkDb;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true"
  }
}
```

#### 4. Créer la base de données

```bash
dotnet ef database update --project src/KBA.Framework.Infrastructure --startup-project src/KBA.Framework.Api
```

#### 5. Lancer l'application

```bash
dotnet run --project src/KBA.Framework.Api
```

L'API sera accessible sur:
- **HTTPS**: https://localhost:5001
- **Swagger**: https://localhost:5001/swagger

### Tester l'API

```bash
# Créer un produit
curl -X POST https://localhost:5001/api/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Mon produit",
    "description": "Description",
    "price": 99.99,
    "stock": 10,
    "sku": "PROD-001",
    "category": "Electronics"
  }'

# Récupérer tous les produits
curl https://localhost:5001/api/products
```

## 📖 Guide complet

### Comment ajouter un nouveau service (10 étapes)

#### Étape 1: Créer l'entité dans Domain

`src/KBA.Framework.Domain/Entities/Orders/Order.cs`

```csharp
namespace KBA.Framework.Domain.Entities.Orders;

public class Order : AggregateRoot<Guid>
{
    public Guid? TenantId { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public DateTime OrderDate { get; private set; }

    private Order() { } // Pour EF Core

    public Order(Guid? tenantId, string orderNumber, decimal totalAmount, Guid? userId = null)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new ArgumentException("Le numéro de commande ne peut pas être vide.");

        Id = Guid.NewGuid();
        TenantId = tenantId;
        OrderNumber = orderNumber;
        TotalAmount = totalAmount;
        OrderDate = DateTime.UtcNow;
        SetCreationInfo(userId);
    }
}
```

#### Étape 2: Créer l'interface du repository

`src/KBA.Framework.Domain/Repositories/IOrderRepository.cs`

```csharp
using KBA.Framework.Domain.Entities.Orders;

namespace KBA.Framework.Domain.Repositories;

public interface IOrderRepository : IRepository<Order, Guid>
{
    Task<List<Order>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
```

#### Étape 3: Créer la configuration EF Core

`src/KBA.Framework.Infrastructure/Data/Configurations/OrderConfiguration.cs`

```csharp
using KBA.Framework.Domain;
using KBA.Framework.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KBA.Framework.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable(KBAConsts.TablePrefix + "Orders");
        
        builder.HasKey(o => o.Id);
        
        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);
            
        builder.Property(o => o.TotalAmount)
            .HasPrecision(18, 2);
            
        builder.HasIndex(o => o.OrderNumber).IsUnique();
        builder.HasIndex(o => o.TenantId);
        
        builder.Ignore(o => o.DomainEvents);
        builder.HasQueryFilter(o => !o.IsDeleted);
    }
}
```

#### Étape 4: Ajouter le DbSet au DbContext

`src/KBA.Framework.Infrastructure/Data/KBADbContext.cs`

```csharp
public DbSet<Order> Orders => Set<Order>();
```

#### Étape 5: Implémenter le repository

`src/KBA.Framework.Infrastructure/Repositories/OrderRepository.cs`

```csharp
using KBA.Framework.Domain.Entities.Orders;
using KBA.Framework.Domain.Repositories;
using KBA.Framework.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KBA.Framework.Infrastructure.Repositories;

public class OrderRepository : Repository<Order, Guid>, IOrderRepository
{
    public OrderRepository(KBADbContext context) : base(context) { }

    public async Task<List<Order>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(o => o.TenantId == tenantId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync(cancellationToken);
    }
}
```

#### Étape 6: Créer les DTOs

`src/KBA.Framework.Application/DTOs/Orders/OrderDto.cs`

```csharp
namespace KBA.Framework.Application.DTOs.Orders;

public record OrderDto(
    Guid Id,
    Guid? TenantId,
    string OrderNumber,
    decimal TotalAmount,
    DateTime OrderDate
);

public record CreateOrderDto(
    string OrderNumber,
    decimal TotalAmount
);
```

#### Étape 7: Créer le service

`src/KBA.Framework.Application/Services/OrderService.cs`

```csharp
using KBA.Framework.Application.DTOs.Orders;
using KBA.Framework.Domain.Entities.Orders;
using KBA.Framework.Domain.Repositories;

namespace KBA.Framework.Application.Services;

public interface IOrderService
{
    Task<OrderDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<OrderDto>> GetListAsync(CancellationToken cancellationToken = default);
    Task<OrderDto> CreateAsync(CreateOrderDto dto, CancellationToken cancellationToken = default);
}

public class OrderService : IOrderService
{
    private readonly IOrderRepository _repository;

    public OrderService(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<OrderDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _repository.GetByIdAsync(id, cancellationToken);
        return order == null ? null : MapToDto(order);
    }

    public async Task<List<OrderDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _repository.GetListAsync(cancellationToken);
        return orders.Select(MapToDto).ToList();
    }

    public async Task<OrderDto> CreateAsync(CreateOrderDto dto, CancellationToken cancellationToken = default)
    {
        var order = new Order(null, dto.OrderNumber, dto.TotalAmount);
        var createdOrder = await _repository.InsertAsync(order, cancellationToken);
        return MapToDto(createdOrder);
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto(
            order.Id,
            order.TenantId,
            order.OrderNumber,
            order.TotalAmount,
            order.OrderDate
        );
    }
}
```

#### Étape 8: Créer le contrôleur

`src/KBA.Framework.Api/Controllers/OrdersController.cs`

```csharp
using KBA.Framework.Application.DTOs.Orders;
using KBA.Framework.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace KBA.Framework.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderDto>>> GetAll(CancellationToken cancellationToken)
    {
        var orders = await _orderService.GetListAsync(cancellationToken);
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetAsync(id, cancellationToken);
        return order == null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderDto dto, CancellationToken cancellationToken)
    {
        var order = await _orderService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }
}
```

#### Étape 9: Enregistrer les services dans Program.cs

```csharp
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
```

#### Étape 10: Créer et appliquer la migration

```bash
dotnet ef migrations add AddOrders --project src/KBA.Framework.Infrastructure --startup-project src/KBA.Framework.Api

dotnet ef database update --project src/KBA.Framework.Infrastructure --startup-project src/KBA.Framework.Api
```

✅ **Votre nouveau service est prêt!**

## 🌐 Déploiement IIS

### Prérequis IIS

1. **ASP.NET Core Hosting Bundle** ([télécharger](https://dotnet.microsoft.com/download/dotnet/8.0))
2. **IIS avec rôles**:
   - IIS Management Console
   - World Wide Web Services
   - Application Development Features → WebSocket Protocol

### Déploiement automatisé

Exécuter le script PowerShell en tant qu'**Administrateur**:

```powershell
.\deploy-iis.ps1
```

### Déploiement manuel

#### 1. Publier l'application

```bash
dotnet publish src/KBA.Framework.Api/KBA.Framework.Api.csproj -c Release -o C:\inetpub\wwwroot\KBAFramework
```

#### 2. Créer le pool d'application

- Ouvrir IIS Manager
- Cliquer sur "Application Pools" → "Add Application Pool"
- Nom: `KBAFrameworkPool`
- .NET CLR Version: `No Managed Code`
- Cliquer OK

#### 3. Créer le site web

- Cliquer sur "Sites" → "Add Website"
- Site name: `KBAFramework`
- Application pool: `KBAFrameworkPool`
- Physical path: `C:\inetpub\wwwroot\KBAFramework`
- Binding: HTTP, Port 8080
- Cliquer OK

#### 4. Configurer les permissions

```powershell
$path = "C:\inetpub\wwwroot\KBAFramework"
$acl = Get-Acl $path
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS AppPool\KBAFrameworkPool", "Modify", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($rule)
Set-Acl $path $acl
```

#### 5. Démarrer le site

Dans IIS Manager, cliquer droit sur le site → "Manage Website" → "Start"

### Vérification

Accéder à: `http://localhost:8080/swagger`

## 🧪 Tests

### Exécuter tous les tests

```bash
dotnet test
```

### Tests par projet

```bash
# Tests du domaine
dotnet test tests/KBA.Framework.Domain.Tests

# Tests de l'application
dotnet test tests/KBA.Framework.Application.Tests

# Tests d'intégration
dotnet test tests/KBA.Framework.Api.IntegrationTests
```

### Couverture de code

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=lcov
```

## 🔧 Troubleshooting

### Problème: La base de données ne se crée pas

**Solution**: Vérifier la chaîne de connexion dans `appsettings.json` et s'assurer que SQL Server LocalDB est installé.

```bash
# Vérifier LocalDB
sqllocaldb info

# Créer une instance si nécessaire
sqllocaldb create MSSQLLocalDB
sqllocaldb start MSSQLLocalDB
```

### Problème: Erreur 500.30 sur IIS

**Solution**: Vérifier que l'ASP.NET Core Hosting Bundle est installé et redémarrer IIS.

```powershell
iisreset
```

Consulter les logs dans: `C:\inetpub\wwwroot\KBAFramework\logs\`

### Problème: Port déjà utilisé

**Solution**: Changer le port dans `appsettings.json` ou `launchSettings.json`

### Problème: Tests d'intégration échouent

**Solution**: S'assurer que la base de données InMemory est correctement configurée dans les tests.

## ✅ Checklist de production

### Avant déploiement

- [ ] Tests unitaires passent à 100%
- [ ] Tests d'intégration passent
- [ ] Chaînes de connexion en variables d'environnement
- [ ] Secrets (API keys, passwords) dans Azure Key Vault ou similaire
- [ ] HTTPS activé et certificat SSL configuré
- [ ] CORS configuré pour les domaines autorisés uniquement
- [ ] Logging configuré (Serilog, Application Insights, etc.)
- [ ] Health checks configurés
- [ ] Rate limiting activé
- [ ] Documentation API à jour

### Sécurité

- [ ] Authentification JWT ou OAuth configurée
- [ ] Autorisation basée sur les rôles/permissions
- [ ] Protection CSRF si nécessaire
- [ ] Validation des entrées utilisateur
- [ ] SQL Injection protégé (utilisez toujours EF Core ou paramètres)
- [ ] XSS protégé (encodage des sorties)
- [ ] Secrets Manager configuré

### Performance

- [ ] Caching configuré (mémoire, Redis)
- [ ] Compression activée
- [ ] CDN pour les assets statiques
- [ ] Index de base de données optimisés
- [ ] Pagination sur les listes
- [ ] Lazy loading configuré correctement

### Monitoring

- [ ] Application Insights ou équivalent
- [ ] Logging centralisé
- [ ] Alertes configurées
- [ ] Dashboard de monitoring
- [ ] Audit logs activés

## 📚 Ressources supplémentaires

- [GUIDE-COMPLET.md](./GUIDE-COMPLET.md) - Documentation approfondie (150+ pages)
- [Microsoft Docs - Clean Architecture](https://docs.microsoft.com/aspnet/core/fundamentals/architecture)
- [Domain-Driven Design Reference](https://www.domainlanguage.com/ddd/)
- [Entity Framework Core Docs](https://docs.microsoft.com/ef/core/)


---

**KBA Framework** - Production-Ready Clean Architecture pour .NET 8
