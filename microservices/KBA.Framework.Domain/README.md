# KBA.Framework.Domain - Microservices Edition

## 🎯 Objectif

Ce projet contient les classes de domaine partagées pour **tous les microservices** KBA Framework.

**⚠️ Important** : Ce projet est **séparé** et **indépendant** du projet `src/KBA.Framework.Domain` utilisé par l'application monolithique.

---

## 📁 Structure

```
KBA.Framework.Domain/
├── Common/
│   ├── Entity.cs                  # Classe de base pour toutes les entités
│   ├── AggregateRoot.cs           # Racine d'agrégat (DDD)
│   ├── ValueObject.cs             # Value Object (DDD)
│   ├── PagedResult.cs             # Pagination standardisée
│   └── ApiResponse.cs             # Réponses API standardisées
├── Entities/
│   ├── User.cs                    # Entité utilisateur
│   ├── Role.cs                    # Entité rôle
│   ├── UserRole.cs                # Table de liaison User-Role
│   ├── Product.cs                 # Entité produit
│   ├── Tenant.cs                  # Entité tenant
│   ├── Permission.cs              # Entité permission
│   └── PermissionGrant.cs         # Attribution de permission
└── KBAConsts.cs                   # Constantes globales
```

---

## 🧩 Classes Communes

### Entity

Classe de base pour toutes les entités du domaine.

```csharp
public abstract class Entity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    
    public void MarkAsDeleted();
    public void MarkAsUpdated();
}
```

**Fonctionnalités** :
- ID unique (Guid)
- Soft delete (IsDeleted)
- Timestamps (CreatedAt, UpdatedAt)
- Equality basée sur l'ID

### AggregateRoot

Racine d'agrégat pour le pattern Domain-Driven Design (DDD).

```csharp
public abstract class AggregateRoot : Entity
{
    public IReadOnlyCollection<object> DomainEvents { get; }
    
    protected void AddDomainEvent(object eventItem);
    public void ClearDomainEvents();
}
```

**Utilisation** :
```csharp
public class Product : AggregateRoot
{
    public void UpdateStock(int quantity)
    {
        Stock = quantity;
        AddDomainEvent(new StockUpdatedEvent(Id, quantity));
    }
}
```

### ValueObject

Classe de base pour les Value Objects (DDD).

```csharp
public abstract class ValueObject
{
    protected abstract IEnumerable<object?> GetEqualityComponents();
    
    public override bool Equals(object? obj);
    public override int GetHashCode();
}
```

**Exemple** :
```csharp
public class Address : ValueObject
{
    public string Street { get; }
    public string City { get; }
    public string ZipCode { get; }
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return City;
        yield return ZipCode;
    }
}
```

### PagedResult<T>

Résultat paginé standardisé pour les API.

```csharp
public class PagedResult<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; }
    public bool HasPreviousPage { get; }
    public bool HasNextPage { get; }
}
```

### ApiResponse<T>

Réponse API standardisée.

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }
    public List<string> Errors { get; set; }
    public string? CorrelationId { get; set; }
    
    public static ApiResponse<T> SuccessResponse(T data, string message = "Success");
    public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null);
}
```

---

## 🏗️ Entités

### User

```csharp
public class User : AggregateRoot
{
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    
    public string FullName { get; }
    public virtual ICollection<UserRole> UserRoles { get; set; }
}
```

### Product

```csharp
public class Product : AggregateRoot
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public string SKU { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; }
    
    public void UpdateStock(int quantity);
    public void DecreaseStock(int quantity);
    public void IncreaseStock(int quantity);
}
```

### Permission

```csharp
public class Permission : Entity
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string? Description { get; set; }
    public string? GroupName { get; set; }
    public Guid? ParentId { get; set; }
    
    public virtual Permission? Parent { get; set; }
    public virtual ICollection<Permission> Children { get; set; }
    public virtual ICollection<PermissionGrant> Grants { get; set; }
}
```

---

## 📦 Utilisation dans les Services

### Référence de Projet

Tous les microservices référencent ce projet :

```xml
<ItemGroup>
  <ProjectReference Include="..\KBA.Framework.Domain\KBA.Framework.Domain.csproj" />
</ItemGroup>
```

### Exemple d'Utilisation

```csharp
using KBA.Framework.Domain.Common;
using KBA.Framework.Domain.Entities;

namespace KBA.ProductService.Data;

public class ProductDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable(KBAConsts.TablePrefix + "Products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(KBAConsts.MaxNameLength);
        });
    }
}
```

---

## 🆚 vs src/KBA.Framework.Domain

| Aspect | **Microservices** | **Monolithe (src)** |
|--------|-------------------|---------------------|
| **Emplacement** | `microservices/KBA.Framework.Domain` | `src/KBA.Framework.Domain` |
| **Utilisation** | Tous les microservices | Application principale |
| **Dépendances** | Minimal (EF Core uniquement) | Peut avoir plus de dépendances |
| **Évolution** | Indépendante | Liée à l'application monolithique |
| **Déploiement** | Avec chaque microservice | Avec l'application principale |

**Avantages de la séparation** :
- ✅ **Indépendance** : Les microservices ne dépendent pas du monolithe
- ✅ **Flexibilité** : Évolution séparée des domaines
- ✅ **Simplicité** : Pas de dépendances inutiles
- ✅ **Déploiement** : Build plus rapide (pas besoin de src/)
- ✅ **Clarté** : Séparation claire des responsabilités

---

## 🔧 Conventions

### Nommage des Tables

Toutes les tables utilisent le préfixe `KBA.` :

```csharp
public const string TablePrefix = "KBA.";

// Résultat en base de données :
// KBA.Users
// KBA.Products
// KBA.Permissions
```

### Longueurs Maximum

```csharp
public const int MaxNameLength = 256;
public const int MaxDescriptionLength = 1024;
public const int MaxEmailLength = 256;
public const int MaxPhoneLength = 24;
```

### Soft Delete

Toutes les entités héritent de `Entity` et supportent le soft delete :

```csharp
product.MarkAsDeleted();
// IsDeleted = true
// DeletedAt = DateTime.UtcNow
```

---

## 🚀 Évolution Future

### Entités à Ajouter

- [ ] **AuditLog** - Logs d'audit
- [ ] **OrganizationUnit** - Unités organisationnelles
- [ ] **Configuration** - Configuration dynamique
- [ ] **Feature** - Feature flags
- [ ] **Notification** - Notifications système

### Améliorations

- [ ] Domain Events dispatcher
- [ ] Specifications pattern
- [ ] Repository pattern interfaces
- [ ] Unit of Work pattern

---

## 📚 Documentation

- **Architecture** : [../docs/ARCHITECTURE.md](../docs/ARCHITECTURE.md)
- **DDD Patterns** : [../docs/DDD-PATTERNS.md](../docs/DDD-PATTERNS.md)
- **Entity Framework** : [../docs/EF-CORE.md](../docs/EF-CORE.md)

---

## ✅ Best Practices

### 1. Immutabilité des Value Objects

```csharp
public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }
    
    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }
    
    // Pas de setters ! Les Value Objects sont immutables
}
```

### 2. Business Logic dans les Entités

```csharp
public class Product : AggregateRoot
{
    // ✅ BON : Logique métier dans l'entité
    public void DecreaseStock(int quantity)
    {
        if (Stock < quantity)
            throw new InvalidOperationException("Stock insuffisant");
        
        Stock -= quantity;
        MarkAsUpdated();
    }
    
    // ❌ MAUVAIS : Setter public sans validation
    // public int Stock { get; set; }
}
```

### 3. Utiliser les Domain Events

```csharp
public class Order : AggregateRoot
{
    public void Complete()
    {
        Status = OrderStatus.Completed;
        AddDomainEvent(new OrderCompletedEvent(Id));
    }
}

// Dans le handler
public class OrderCompletedEventHandler
{
    public async Task Handle(OrderCompletedEvent @event)
    {
        // Envoyer email
        // Mettre à jour le stock
        // etc.
    }
}
```

---

## 🎉 Résumé

**KBA.Framework.Domain** pour les microservices :
- ✅ Projet séparé et indépendant
- ✅ Classes de domaine partagées
- ✅ Patterns DDD (Entity, AggregateRoot, ValueObject)
- ✅ Utilitaires communs (Pagination, ApiResponse)
- ✅ 6 entités prêtes à l'emploi
- ✅ Conventions claires

**Utilisé par** : Identity, Product, Tenant, Permission Services  
**Version** : 2.0  
**Status** : Production Ready ✅
