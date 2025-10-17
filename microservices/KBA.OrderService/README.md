# 🛒 OrderService - Exemple Complet d'Orchestration Multi-Services

## 🎯 Objectif

Ce service démontre comment **un service dépend de PLUSIEURS autres services** via gRPC :

- ✅ **IdentityService** - Vérifier l'existence des utilisateurs
- ✅ **PermissionService** - Vérifier les autorisations
- ✅ **ProductService** (à implémenter) - Vérifier la disponibilité des produits

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      OrderService                            │
│                     (Port 5005)                              │
│                                                              │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  CreateOrder / ValidateOrder                         │   │
│  │  ↓                                                    │   │
│  │  1. Appel gRPC → IdentityService                     │   │
│  │     ↓ Vérifier que l'utilisateur existe              │   │
│  │                                                       │   │
│  │  2. Appel gRPC → PermissionService                   │   │
│  │     ↓ Vérifier permission "Orders.Create"            │   │
│  │                                                       │   │
│  │  3. Appel gRPC → ProductService (optionnel)          │   │
│  │     ↓ Vérifier disponibilité du stock                │   │
│  │                                                       │   │
│  │  4. Créer la commande dans la base de données        │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

## 📝 Fichier .proto

**`KBA.Framework.Grpc/Protos/order.proto`**

Définit le contrat gRPC pour OrderService (créé MANUELLEMENT) :

```protobuf
service OrderService {
  rpc CreateOrder (CreateOrderRequest) returns (CreateOrderResponse);
  rpc GetOrder (GetOrderRequest) returns (GetOrderResponse);
  rpc GetUserOrders (GetUserOrdersRequest) returns (GetUserOrdersResponse);
  rpc ValidateOrder (ValidateOrderRequest) returns (ValidateOrderResponse);
}
```

## 💻 Code Important

### Orchestration Multi-Services

**`Services/OrderServiceLogic.cs`**

```csharp
public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto)
{
    // 1️⃣ Appel gRPC vers IdentityService
    var userExists = await CheckUserExistsAsync(dto.UserId, correlationId);
    if (!userExists)
        throw new InvalidOperationException("User not found");

    // 2️⃣ Appel gRPC vers PermissionService
    var hasPermission = await CheckPermissionAsync(dto.UserId, "Orders.Create", ...);
    if (!hasPermission)
        throw new UnauthorizedAccessException("No permission");

    // 3️⃣ TODO: Appel gRPC vers ProductService
    // var productsAvailable = await CheckProductsAsync(dto.Items);

    // 4️⃣ Créer la commande
    var order = new Order(dto.UserId, dto.TenantId, dto.ShippingAddress);
    // ...
    
    return MapToDto(order);
}
```

### Clients gRPC Intégrés

```csharp
private async Task<bool> CheckUserExistsAsync(Guid userId, string correlationId)
{
    // Créer le channel gRPC
    var grpcUrl = _configuration["GrpcServices:IdentityService"];
    using var channel = GrpcChannel.ForAddress(grpcUrl);
    
    // Créer le client (généré automatiquement depuis identity.proto)
    var client = new IdentityService.IdentityServiceClient(channel);
    
    // Faire l'appel gRPC
    var response = await client.UserExistsAsync(new UserExistsRequest
    {
        UserId = userId.ToString(),
        CorrelationId = correlationId
    });
    
    return response.Exists;
}
```

## 🚀 Démarrage

### 1. Configuration

**`appsettings.json`**

```json
{
  "GrpcServices": {
    "IdentityService": "http://localhost:5001",
    "PermissionService": "http://localhost:5004",
    "ProductService": "http://localhost:5002"
  }
}
```

### 2. Migration

```powershell
cd KBA.OrderService
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 3. Démarrer le Service

```powershell
dotnet run
```

Le service démarre sur **http://localhost:5005**

## 🧪 Tester

### Test 1 : Créer une Commande

```powershell
# 1. S'authentifier
$loginResponse = Invoke-RestMethod -Uri "http://localhost:5000/api/identity/auth/login" `
    -Method Post `
    -ContentType "application/json" `
    -Body '{"email":"admin@kba.com","password":"Admin123!"}'

$token = $loginResponse.token

# 2. Créer une commande
$headers = @{ "Authorization" = "Bearer $token" }

$orderRequest = @{
    userId = "00000000-0000-0000-0000-000000000001"
    shippingAddress = "123 Rue Test, Paris"
    items = @(
        @{
            productId = "00000000-0000-0000-0000-000000000001"
            quantity = 2
            unitPrice = 99.99
        }
    )
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "http://localhost:5005/api/orders" `
    -Method Post `
    -Headers $headers `
    -ContentType "application/json" `
    -Body $orderRequest

Write-Host "✅ Commande créée : $($response.data.id)"
```

### Test 2 : Valider une Commande

```powershell
$validateRequest = @{
    userId = "00000000-0000-0000-0000-000000000001"
    items = @(
        @{
            productId = "00000000-0000-0000-0000-000000000001"
            quantity = 2
            unitPrice = 99.99
        }
    )
} | ConvertTo-Json

$validation = Invoke-RestMethod -Uri "http://localhost:5005/api/orders/validate" `
    -Method Post `
    -Headers $headers `
    -ContentType "application/json" `
    -Body $validateRequest

Write-Host "Validation:"
Write-Host "  ✓ User exists: $($validation.data.userExists)"
Write-Host "  ✓ Has permission: $($validation.data.hasPermission)"
Write-Host "  ✓ Products available: $($validation.data.productsAvailable)"
```

## 📊 Logs Détaillés

Quand vous créez une commande, vous verrez dans les logs :

**OrderService :**
```
[INFO] Creating order for user xxx with 2 items [CorrelationId: yyy]
[INFO] Step 1/3: Checking user existence via IdentityService gRPC...
[INFO] IdentityService gRPC response: User xxx exists = True
[INFO] ✅ User exists
[INFO] Step 2/3: Checking permissions via PermissionService gRPC...
[INFO] PermissionService gRPC response: User xxx has permission 'Orders.Create' = True
[INFO] ✅ User has required permissions
[INFO] Step 3/3: Checking product availability (simulated)...
[INFO] Order zzz created successfully
```

**IdentityService :**
```
[INFO] gRPC UserExists: UserId=xxx, CorrelationId=yyy
```

**PermissionService :**
```
[INFO] gRPC CheckPermission: UserId=xxx, Permission=Orders.Create, CorrelationId=yyy
```

## 🔍 Points Clés

### 1. Fichiers .proto Partagés

Tous les fichiers `.proto` sont dans **`KBA.Framework.Grpc/Protos/`**

- `identity.proto` - Défini manuellement
- `permission.proto` - Défini manuellement
- `order.proto` - Défini manuellement

### 2. Code Généré Automatiquement

Lors du `dotnet build`, les classes C# sont générées :

- `IdentityService.IdentityServiceClient` - Client pour IdentityService
- `PermissionService.PermissionServiceClient` - Client pour PermissionService
- `OrderService.OrderServiceBase` - Serveur pour OrderService

### 3. Configuration Centralisée

```json
{
  "GrpcServices": {
    "IdentityService": "http://localhost:5001",
    "PermissionService": "http://localhost:5004"
  }
}
```

Facile à changer pour la production !

## 📚 Structure du Projet

```
KBA.OrderService/
├── Controllers/
│   └── OrdersController.cs        # REST API
├── DTOs/
│   └── OrderDtos.cs               # Data Transfer Objects
├── Data/
│   └── OrderDbContext.cs          # Entity Framework
├── Entities/
│   └── Order.cs                   # Entités métier
├── Grpc/
│   └── OrderGrpcService.cs        # Serveur gRPC
├── Services/
│   └── OrderServiceLogic.cs       # 🎯 ORCHESTRATION MULTI-SERVICES
├── Program.cs                     # Configuration
└── appsettings.json              # Configuration gRPC
```

## 🎓 Ce que Vous Apprenez

1. ✅ Comment appeler **plusieurs services gRPC** depuis un même service
2. ✅ Comment gérer le **CorrelationId** pour le traçage
3. ✅ Comment créer un **fichier .proto manuellement**
4. ✅ Comment utiliser le **code généré automatiquement**
5. ✅ Comment **orchestrer** plusieurs appels asynchrones
6. ✅ Comment gérer les **erreurs gRPC**

## 🔄 Workflow Complet

```
1. Créer order.proto (MANUEL)
   ↓
2. dotnet build (AUTOMATIQUE - génère le code C#)
   ↓
3. Implémenter OrderGrpcService (MANUEL - serveur)
   ↓
4. Utiliser les clients gRPC (MANUEL - clients)
   ↓
5. Tester via REST ou gRPC
```

## 🚀 Prochaines Étapes

Pour compléter l'exemple, vous pourriez :

1. **Ajouter ProductService gRPC** pour vérifier le stock
2. **Implémenter TenantService gRPC** pour valider le tenant
3. **Ajouter un cache Redis** pour les vérifications fréquentes
4. **Implémenter le pattern Saga** pour les transactions distribuées

## 📖 Documentation Associée

- [COMPRENDRE-PROTOBUF.md](../docs/COMPRENDRE-PROTOBUF.md) - Guide complet sur les .proto
- [GRPC-COMMUNICATION.md](../docs/GRPC-COMMUNICATION.md) - Communication gRPC
- [CHANGELOG-V3.md](../docs/CHANGELOG-V3.md) - Nouveautés v3.0

---

**OrderService = Exemple pratique d'orchestration multi-services avec gRPC** 🎯
