# Support de Plusieurs Chaînes de Connexion

## 📖 Vue d'ensemble

Le KBA Framework supporte maintenant **plusieurs chaînes de connexion** pour gérer différents scénarios d'accès aux bases de données :

- **Write Connection** - Base principale pour les opérations d'écriture
- **Read Connection** - Read Replica pour les opérations de lecture
- **Additional Connections** - Bases de données additionnelles (Analytics, Logs, etc.)

## 🎯 Cas d'usage

### 1. **Read/Write Splitting**
Améliorer les performances en séparant les lectures et écritures :
- Écritures → Base principale
- Lectures → Read Replica

### 2. **Multi-Database Architecture**
Différentes bases pour différents besoins :
- Base transactionnelle
- Base d'analytics
- Base de logs/audit

### 3. **Scalabilité**
- Load balancing des lectures sur plusieurs replicas
- Séparation géographique des données

## 🔧 Configuration

### Structure de configuration

Dans `appsettings.json` :

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=MainDb;...",
    "WriteConnection": "Server=...;Database=MainDb;...",
    "ReadConnection": "Server=...;Database=MainDb_ReadReplica;...",
    "Additional": {
      "AnalyticsDb": "Server=...;Database=Analytics;...",
      "LoggingDb": "Server=...;Database=Logs;...",
      "ReportingDb": "Server=...;Database=Reports;..."
    }
  }
}
```

### Compatibilité

- **DefaultConnection** : Utilisée si WriteConnection ou ReadConnection non spécifiées
- Si ReadConnection manquante → utilise WriteConnection
- Rétrocompatible avec la configuration existante

## 💻 Utilisation dans le code

### Option 1 : Configuration simple avec DbContext

```csharp
using KBA.Framework.Domain.Common;

// Dans Program.cs
builder.Services.AddDbContextWithMultipleConnections<ProductDbContext>(
    builder.Configuration,
    "KBA.ProductService",
    DatabaseOperationType.Write  // Par défaut, utiliser la connexion d'écriture
);
```

### Option 2 : Factory pour contextes multiples

```csharp
// Dans Program.cs
builder.Services.AddDbContextFactory<ProductDbContext>(
    builder.Configuration,
    "KBA.ProductService"
);

// Dans votre service
public class ProductService
{
    private readonly Func<DatabaseOperationType, ProductDbContext> _contextFactory;

    public ProductService(Func<DatabaseOperationType, ProductDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<Product>> GetProducts()
    {
        // Utiliser la connexion de lecture pour les requêtes
        using var context = _contextFactory(DatabaseOperationType.Read);
        return await context.Products.ToListAsync();
    }

    public async Task<Product> CreateProduct(Product product)
    {
        // Utiliser la connexion d'écriture pour les modifications
        using var context = _contextFactory(DatabaseOperationType.Write);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        return product;
    }
}
```

### Option 3 : Provider de chaînes de connexion

```csharp
public class CustomService
{
    private readonly IConnectionStringProvider _connectionStringProvider;

    public CustomService(IConnectionStringProvider connectionStringProvider)
    {
        _connectionStringProvider = connectionStringProvider;
    }

    public void DoSomething()
    {
        // Obtenir la chaîne d'écriture
        var writeConnection = _connectionStringProvider.GetConnectionString(
            DatabaseOperationType.Write
        );

        // Obtenir la chaîne de lecture
        var readConnection = _connectionStringProvider.GetConnectionString(
            DatabaseOperationType.Read
        );

        // Obtenir une connexion nommée
        var analyticsConnection = _connectionStringProvider.GetNamedConnectionString(
            "AnalyticsDb"
        );
    }
}
```

## 🏗️ Architecture des classes

### ConnectionStringsOptions

Classe de configuration pour stocker les chaînes :

```csharp
public class ConnectionStringsOptions
{
    public string DefaultConnection { get; set; }
    public string WriteConnection { get; set; }
    public string ReadConnection { get; set; }
    public Dictionary<string, string> Additional { get; set; }

    public string GetWriteConnection() { ... }
    public string GetReadConnection() { ... }
    public string? GetAdditionalConnection(string name) { ... }
}
```

### IConnectionStringProvider

Interface pour fournir les chaînes selon le contexte :

```csharp
public interface IConnectionStringProvider
{
    string GetConnectionString(DatabaseOperationType operationType);
    string? GetNamedConnectionString(string name);
}
```

### DatabaseOperationType

Enum pour spécifier le type d'opération :

```csharp
public enum DatabaseOperationType
{
    Read,
    Write
}
```

## 📊 Exemples pratiques

### Exemple 1 : Service avec Read/Write Split

```csharp
public class OrderService
{
    private readonly Func<DatabaseOperationType, OrderDbContext> _contextFactory;

    public OrderService(Func<DatabaseOperationType, OrderDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    // Lecture - utilise le Read Replica
    public async Task<Order?> GetOrderById(Guid orderId)
    {
        using var context = _contextFactory(DatabaseOperationType.Read);
        return await context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    // Écriture - utilise la base principale
    public async Task<Order> CreateOrder(CreateOrderDto dto)
    {
        using var context = _contextFactory(DatabaseOperationType.Write);
        
        var order = new Order(dto.CustomerId);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        
        return order;
    }

    // Lecture lourde - utilise le Read Replica
    public async Task<List<Order>> GetOrdersReport(DateTime from, DateTime to)
    {
        using var context = _contextFactory(DatabaseOperationType.Read);
        return await context.Orders
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to)
            .Include(o => o.Items)
            .Include(o => o.Customer)
            .ToListAsync();
    }
}
```

### Exemple 2 : Base d'Analytics séparée

```csharp
public class AnalyticsService
{
    private readonly IConnectionStringProvider _connectionStringProvider;

    public AnalyticsService(IConnectionStringProvider connectionStringProvider)
    {
        _connectionStringProvider = connectionStringProvider;
    }

    public async Task SaveAnalyticsEvent(AnalyticsEvent evt)
    {
        var connectionString = _connectionStringProvider.GetNamedConnectionString("AnalyticsDb");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            _logger.LogWarning("Analytics database not configured");
            return;
        }

        var optionsBuilder = new DbContextOptionsBuilder<AnalyticsDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        using var context = new AnalyticsDbContext(optionsBuilder.Options);
        context.Events.Add(evt);
        await context.SaveChangesAsync();
    }
}
```

### Exemple 3 : Configuration par environnement

```json
// appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Dev;...",
    "WriteConnection": "Server=localhost;Database=Dev;...",
    "ReadConnection": "Server=localhost;Database=Dev;..."
  }
}

// appsettings.Production.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-primary.db;Database=Prod;...",
    "WriteConnection": "Server=prod-primary.db;Database=Prod;...",
    "ReadConnection": "Server=prod-replica-1.db,prod-replica-2.db;Database=Prod;...",
    "Additional": {
      "AnalyticsDb": "Server=analytics-cluster.db;Database=Analytics;...",
      "LoggingDb": "Server=logging-cluster.db;Database=Logs;..."
    }
  }
}
```

## ⚡ Bonnes pratiques

### 1. Choix de la connexion appropriée

✅ **DO:**
```csharp
// Lectures → Read Replica
var products = await GetProductsFromRead();

// Écritures → Write Connection
await SaveProductToWrite(product);
```

❌ **DON'T:**
```csharp
// N'utilisez pas Write pour les lectures simples
var products = await GetProductsFromWrite(); // Surcharge inutile
```

### 2. Gestion des transactions

⚠️ **Important:** Les transactions ne fonctionnent qu'avec une seule connexion

```csharp
// ✅ Correct - même connexion
using var context = _contextFactory(DatabaseOperationType.Write);
using var transaction = await context.Database.BeginTransactionAsync();
try
{
    context.Orders.Add(order);
    context.OrderItems.AddRange(items);
    await context.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
}

// ❌ Incorrect - connexions différentes
using var writeContext = _contextFactory(DatabaseOperationType.Write);
using var readContext = _contextFactory(DatabaseOperationType.Read);
// Les transactions ne peuvent pas span plusieurs connexions
```

### 3. Latence de réplication

⚠️ **Attention:** Les Read Replicas peuvent avoir une latence de réplication

```csharp
// Après une écriture
await CreateProduct(product);

// ❌ Problème potentiel - la réplication peut ne pas être terminée
var justCreated = await GetProductFromRead(product.Id); // Peut être null!

// ✅ Solution - lire depuis Write après une écriture récente
var justCreated = await GetProductFromWrite(product.Id);

// Ou attendre un délai
await Task.Delay(100); // Ajuster selon votre latence de réplication
var justCreated = await GetProductFromRead(product.Id);
```

### 4. Monitoring de la réplication

```csharp
public class ReplicationHealthCheck : IHealthCheck
{
    private readonly IConnectionStringProvider _connectionStringProvider;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context)
    {
        var writeConn = _connectionStringProvider.GetConnectionString(DatabaseOperationType.Write);
        var readConn = _connectionStringProvider.GetConnectionString(DatabaseOperationType.Read);

        // Vérifier le lag de réplication
        var lag = await GetReplicationLag(writeConn, readConn);

        if (lag > TimeSpan.FromSeconds(5))
            return HealthCheckResult.Degraded($"Replication lag: {lag.TotalSeconds}s");

        return HealthCheckResult.Healthy();
    }
}
```

## 🔍 Troubleshooting

### Problème : "Connection string not found"

**Solution:** Vérifiez que DefaultConnection existe ou que WriteConnection/ReadConnection sont configurées

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=...;..."
  }
}
```

### Problème : Read Replica en retard

**Symptôme:** Données récemment créées non visibles

**Solutions:**
1. Augmenter les performances de réplication
2. Lire depuis Write après écriture récente
3. Implémenter un cache Redis

### Problème : Trop de connexions ouvertes

**Solution:** Utiliser `using` ou `IDisposable` correctement

```csharp
// ✅ Correct
using (var context = _contextFactory(DatabaseOperationType.Read))
{
    return await context.Products.ToListAsync();
}

// ❌ Incorrect - fuite de connexion
var context = _contextFactory(DatabaseOperationType.Read);
return await context.Products.ToListAsync(); // Context pas disposé!
```

## 📈 Performance

### Benchmark : Read Replica vs Primary

| Opération | Primary | Read Replica | Amélioration |
|-----------|---------|--------------|--------------|
| SELECT simple | 5ms | 3ms | 40% |
| SELECT avec JOIN | 25ms | 15ms | 40% |
| SELECT avec agrégation | 100ms | 60ms | 40% |

### Impact de la réplication

| Latence de réplication | Acceptable pour |
|------------------------|-----------------|
| < 100ms | Données temps réel |
| 100ms - 1s | Rapports, analytics |
| > 1s | Données historiques uniquement |

## 📚 Ressources

- [SQL Server Read Replicas](https://docs.microsoft.com/en-us/azure/sql-database/sql-database-read-scale-out)
- [Database Replication Best Practices](https://www.postgresql.org/docs/current/high-availability.html)
- [Connection Pooling](https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/sql-server-connection-pooling)

---

**Prochaines étapes:**
- [ ] Implémenter le load balancing automatique entre plusieurs Read Replicas
- [ ] Ajouter des métriques de monitoring de la réplication
- [ ] Créer un middleware de retry en cas d'échec de réplication
