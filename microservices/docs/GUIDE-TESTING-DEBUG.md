# 🔍 Guide Testing & Debugging Microservices

## 📋 Table des Matières

1. [Tester avec Postman](#tester-avec-postman)
2. [Communication Entre Services](#communication-entre-services)
3. [Debugging Multi-Services](#debugging-multi-services)
4. [Scénarios Réels](#scénarios-réels)

---

## 🧪 Tester avec Postman

### Étape 1 : Démarrer les Services

```powershell
cd microservices
.\start-microservices.ps1
```

**Résultat** : 5 fenêtres PowerShell s'ouvrent (un par service)

### Étape 2 : Vérifier que Tout Tourne

**Health Checks** (dans Postman) :
```
GET http://localhost:5001/health  → Identity Service
GET http://localhost:5002/health  → Product Service
GET http://localhost:5003/health  → Tenant Service
GET http://localhost:5004/health  → Permission Service
GET http://localhost:5000/health  → API Gateway
```

**Tous doivent retourner** : `200 OK` avec `"Healthy"`

### Étape 3 : Authentification (Obtenir un Token JWT)

#### A. Créer un Compte

**Request** :
```http
POST http://localhost:5001/api/auth/register
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "Test123!",
  "firstName": "Test",
  "lastName": "User"
}
```

**Response** :
```json
{
  "id": "abc-123-...",
  "email": "test@example.com",
  "token": "eyJhbGciOiJIUzI1NiIs..."  // ← Copier ce token
}
```

#### B. Se Connecter

**Request** :
```http
POST http://localhost:5001/api/auth/login
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "Test123!"
}
```

**Response** :
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "email": "test@example.com",
  "userId": "abc-123-..."
}
```

### Étape 4 : Utiliser le Token dans Postman

#### Méthode 1 : Header Manuel

Dans chaque requête, ajouter :
```
Headers:
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

#### Méthode 2 : Variables Postman (Recommandé)

1. **Créer une collection** "KBA Microservices"
2. **Variables** tab → Ajouter :
   - `base_url`: `http://localhost:5000`
   - `token`: `eyJhbGciOiJIUzI1NiIs...`
3. **Authorization** tab → Type: `Bearer Token` → Token: `{{token}}`

**Dans les requêtes** :
```
GET {{base_url}}/api/products
```

### Étape 5 : Tester les Endpoints

#### Sans Authentification (Public)

```http
# Lister les produits
GET http://localhost:5002/api/products

# Rechercher
GET http://localhost:5002/api/products/search?searchTerm=iPhone

# Obtenir les catégories
GET http://localhost:5002/api/products/categories

# Lister les permissions
GET http://localhost:5004/api/permissions
```

#### Avec Authentification (Privé)

```http
# Créer un produit
POST http://localhost:5002/api/products
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "name": "Test Product",
  "description": "Created via Postman",
  "sku": "TEST-001",
  "price": 99.99,
  "stock": 10,
  "category": "Test"
}

# Mettre à jour le stock
PATCH http://localhost:5002/api/products/{id}/stock
Authorization: Bearer {{token}}
Content-Type: application/json

50
```

---

## 🔗 Communication Entre Services

### Scénario : Product Service → Permission Service

**Contexte** : Avant de créer un produit, vérifier si l'utilisateur a la permission `Products.Create`

### Implémentation

#### 1. Configuration dans appsettings.json

**Product Service** (`appsettings.json`) :
```json
{
  "ExternalServices": {
    "PermissionServiceUrl": "http://localhost:5004"
  }
}
```

#### 2. Enregistrer HttpClient dans Program.cs

**Product Service** (`Program.cs`) :
```csharp
// Enregistrer le client HTTP pour Permission Service
builder.Services.AddHttpClient("PermissionService", (serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var baseUrl = config["ExternalServices:PermissionServiceUrl"];
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

#### 3. Créer un Service Client

**Product Service** (`Services/PermissionServiceClient.cs`) :
```csharp
namespace KBA.ProductService.Services;

public interface IPermissionServiceClient
{
    Task<bool> CheckPermissionAsync(Guid userId, string permissionName);
}

public class PermissionServiceClient : IPermissionServiceClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PermissionServiceClient> _logger;

    public PermissionServiceClient(
        IHttpClientFactory httpClientFactory, 
        ILogger<PermissionServiceClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> CheckPermissionAsync(Guid userId, string permissionName)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("PermissionService");
            
            var request = new
            {
                userId = userId,
                permissionName = permissionName,
                tenantId = (Guid?)null
            };

            var response = await client.PostAsJsonAsync("/api/permissions/check", request);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Permission check failed: {StatusCode}", 
                    response.StatusCode
                );
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<PermissionCheckResult>();
            return result?.IsGranted ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, 
                "Error checking permission {PermissionName} for user {UserId}", 
                permissionName, 
                userId
            );
            return false; // Fail-safe : refuse si erreur
        }
    }
}

public class PermissionCheckResult
{
    public bool IsGranted { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public string? GrantedBy { get; set; }
}
```

#### 4. Enregistrer le Service Client

**Product Service** (`Program.cs`) :
```csharp
// Après AddHttpClient
builder.Services.AddScoped<IPermissionServiceClient, PermissionServiceClient>();
```

#### 5. Utiliser dans le Controller

**Product Service** (`Controllers/ProductsController.cs`) :
```csharp
public class ProductsController : ControllerBase
{
    private readonly IProductServiceLogic _productService;
    private readonly IPermissionServiceClient _permissionService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductServiceLogic productService,
        IPermissionServiceClient permissionService,
        ILogger<ProductsController> logger)
    {
        _productService = productService;
        _permissionService = permissionService;
        _logger = logger;
    }

    [HttpPost]
    [Authorize] // L'utilisateur doit être authentifié
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
    {
        // 1. Récupérer l'ID utilisateur du token JWT
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized("User ID not found in token");
        }

        // 2. Vérifier la permission via Permission Service
        var hasPermission = await _permissionService.CheckPermissionAsync(
            userId, 
            "Products.Create"
        );

        if (!hasPermission)
        {
            _logger.LogWarning(
                "User {UserId} tried to create product without permission", 
                userId
            );
            return Forbid(); // 403 Forbidden
        }

        // 3. Créer le produit
        try
        {
            var product = await _productService.CreateAsync(dto);
            _logger.LogInformation(
                "Product created: {ProductId} by user {UserId}", 
                product.Id, 
                userId
            );
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
```

### Test dans Postman

**Sans Permission** :
```http
POST http://localhost:5002/api/products
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "name": "Test Product",
  "sku": "TEST-001",
  "price": 99.99,
  "stock": 10
}

# Résultat : 403 Forbidden (pas de permission)
```

**Accorder la Permission** :
```http
POST http://localhost:5004/api/permissions/grant
Authorization: Bearer {{admin_token}}
Content-Type: application/json

{
  "permissionName": "Products.Create",
  "providerName": "User",
  "providerKey": "USER_GUID",
  "tenantId": null
}
```

**Réessayer** :
```http
POST http://localhost:5002/api/products
# Résultat : 201 Created ✅
```

---

## 🐛 Debugging Multi-Services

### Problème : 2 Projets Différents

Oui, ce sont **2 projets .NET différents** qui tournent dans des **processus séparés**.

### Solution : Debug Multiple Projects dans Visual Studio

#### Méthode 1 : Configure Multiple Startup Projects

1. **Clic droit sur la solution** `KBA.Microservices.sln`
2. **"Set Startup Projects..."**
3. Sélectionner **"Multiple startup projects"**
4. **Pour chaque service à débugger** :
   - Action: `Start`
   - Identity Service
   - Product Service
   - Permission Service
   - (Pas besoin du Gateway pour debug)
5. **OK**

6. **Appuyer sur F5** → Tous les services démarrent en debug !

#### Méthode 2 : Attach to Process (Recommandé)

**Plus flexible pour debugging ciblé** :

1. **Démarrer les services normalement** :
   ```powershell
   .\start-microservices.ps1
   ```

2. **Dans Visual Studio** :
   - Menu `Debug` → `Attach to Process...` (Ctrl+Alt+P)
   - Filtrer : `dotnet.exe`
   - Sélectionner les processus :
     - `KBA.ProductService.exe` ou `dotnet exec KBA.ProductService.dll`
     - `KBA.PermissionService.exe` ou `dotnet exec KBA.PermissionService.dll`
   - **Cliquer "Attach"**

3. **Mettre des breakpoints** dans les 2 services

4. **Envoyer la requête** depuis Postman

**Le debugger s'arrêtera** :
- D'abord dans `ProductService` (point d'entrée)
- Puis dans `PermissionService` (quand appelé)

### Scénario de Debug Complet

#### Étape 1 : Mettre des Breakpoints

**Product Service** (`Controllers/ProductsController.cs`) :
```csharp
[HttpPost]
public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
{
    var userId = ...; // ← BREAKPOINT 1
    
    var hasPermission = await _permissionService.CheckPermissionAsync(
        userId, 
        "Products.Create"
    ); // ← BREAKPOINT 2
    
    if (!hasPermission) // ← BREAKPOINT 3
    {
        return Forbid();
    }
    
    var product = await _productService.CreateAsync(dto); // ← BREAKPOINT 4
    return CreatedAtAction(...);
}
```

**Permission Service** (`Controllers/PermissionsController.cs`) :
```csharp
[HttpPost("check")]
public async Task<ActionResult<PermissionCheckResult>> CheckPermission(
    [FromBody] CheckPermissionDto dto)
{
    var result = await _permissionService.CheckPermissionAsync(dto); // ← BREAKPOINT 5
    return Ok(result); // ← BREAKPOINT 6
}
```

#### Étape 2 : Attacher les Debuggers

```
Attach to Process → dotnet.exe (ProductService)
Attach to Process → dotnet.exe (PermissionService)
```

#### Étape 3 : Envoyer la Requête Postman

```http
POST http://localhost:5002/api/products
Authorization: Bearer {{token}}
...
```

#### Étape 4 : Suivre le Flow

**Debugger s'arrête dans cet ordre** :

1. ✅ **Product Service - BREAKPOINT 1** : `var userId = ...`
   - Inspecter : `User.Claims`, vérifier le token JWT
   - F10 (Step Over)

2. ✅ **Product Service - BREAKPOINT 2** : Avant l'appel à Permission Service
   - Inspecter : `userId`, `permissionName`
   - F11 (Step Into) → Va entrer dans `CheckPermissionAsync`

3. ✅ **Product Service - PermissionServiceClient** : Construction de la requête HTTP
   - Inspecter : `request` (DTO envoyé)
   - F10 jusqu'à `PostAsJsonAsync`

4. 🔄 **HTTP Call** → Product Service attend la réponse

5. ✅ **Permission Service - BREAKPOINT 5** : Reçoit la requête
   - Inspecter : `dto.UserId`, `dto.PermissionName`
   - F10 (Step Over)

6. ✅ **Permission Service - BREAKPOINT 6** : Retourne le résultat
   - Inspecter : `result.IsGranted`
   - F5 (Continue)

7. 🔄 **HTTP Response** → Product Service reçoit la réponse

8. ✅ **Product Service - BREAKPOINT 2** : Après l'appel
   - Inspecter : `hasPermission` (true/false)
   - F10

9. ✅ **Product Service - BREAKPOINT 3** : Check if permission
   - Si `true` → Skip
   - Si `false` → Entre dans le `if` et retourne `Forbid()`

10. ✅ **Product Service - BREAKPOINT 4** : Création du produit
    - F11 pour entrer dans `CreateAsync`

#### Étape 5 : Analyser

**Call Stack Window** (Alt+7) montre :
```
ProductsController.Create()
  ↓
PermissionServiceClient.CheckPermissionAsync()
  ↓
HttpClient.PostAsJsonAsync()
  ↓
[HTTP Request to Permission Service]
  ↓
PermissionsController.CheckPermission()
  ↓
PermissionServiceLogic.CheckPermissionAsync()
```

**Locals Window** (Alt+4) : Variables locales  
**Watch Window** (Alt+3) : Variables surveillées  
**Output Window** : Logs Serilog

### Debugging Tips

#### 1. Logger pour Tracer les Appels

```csharp
_logger.LogInformation(
    "Calling Permission Service for user {UserId} and permission {Permission}",
    userId,
    permissionName
);

var result = await _permissionService.CheckPermissionAsync(userId, permissionName);

_logger.LogInformation(
    "Permission check result: {IsGranted}",
    result
);
```

**Dans Output Window** :
```
[12:34:56 INF] [ProductService] Calling Permission Service for user abc-123 and permission Products.Create
[12:34:57 INF] [PermissionService] Checking permission Products.Create for user abc-123
[12:34:57 INF] [PermissionService] Permission NOT granted (no grant found)
[12:34:57 INF] [ProductService] Permission check result: False
```

#### 2. Correlation ID pour Tracer

**Grâce au `CorrelationIdMiddleware`** déjà implémenté :

```csharp
// Dans Product Service
var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
_logger.LogInformation(
    "CorrelationId: {CorrelationId} - Calling Permission Service",
    correlationId
);

// Passer le Correlation ID dans la requête HTTP
var client = _httpClientFactory.CreateClient("PermissionService");
client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
```

**Logs dans les 2 services** :
```
[ProductService] [CorrelationId: abc-xyz] Calling Permission Service
[PermissionService] [CorrelationId: abc-xyz] Received permission check
```

**Maintenant vous pouvez filtrer dans Seq** :
```
CorrelationId = "abc-xyz"
```
→ Voir toute la trace de la requête à travers les 2 services !

#### 3. Conditional Breakpoints

**Clic droit sur le breakpoint** → "Conditions..." :
```csharp
// S'arrêter seulement pour un utilisateur spécifique
userId == Guid.Parse("abc-123-...")

// S'arrêter seulement si permission refusée
!hasPermission
```

---

## 🎯 Scénarios Réels

### Scénario 1 : "Pourquoi mon produit n'est pas créé ?"

**Symptoms** : POST retourne 403 Forbidden

**Debug Steps** :

1. **Vérifier le token JWT** :
   ```
   BREAKPOINT → var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
   Inspecter : userId (est-il null ?)
   ```

2. **Vérifier l'appel Permission Service** :
   ```
   BREAKPOINT → var hasPermission = await _permissionService.CheckPermissionAsync(...)
   Step Into (F11) pour voir l'appel HTTP
   ```

3. **Vérifier la réponse** :
   ```
   BREAKPOINT après l'appel
   Inspecter : hasPermission (true/false ?)
   ```

4. **Si false, vérifier dans Permission Service** :
   ```
   Attach Permission Service
   BREAKPOINT dans CheckPermissionAsync
   Vérifier : Existe-t-il un grant pour cet utilisateur ?
   ```

5. **Solution** : Accorder la permission
   ```http
   POST http://localhost:5004/api/permissions/grant
   ```

### Scénario 2 : "Timeout - Permission Service ne répond pas"

**Symptoms** : Exception `TaskCanceledException` après 30 secondes

**Debug Steps** :

1. **Vérifier que Permission Service tourne** :
   ```http
   GET http://localhost:5004/health
   ```

2. **Vérifier l'URL dans config** :
   ```csharp
   BREAKPOINT dans PermissionServiceClient
   Inspecter : client.BaseAddress (bonne URL ?)
   ```

3. **Vérifier les logs** :
   ```
   Output Window → Chercher "Exception"
   Logs/permission-service-.log
   ```

4. **Solution** : Redémarrer Permission Service

### Scénario 3 : "J'obtiens une exception dans Permission Service"

**Symptoms** : 500 Internal Server Error

**Debug Steps** :

1. **Attach Permission Service debugger**

2. **Enable Break on Exceptions** :
   - Debug → Windows → Exception Settings (Ctrl+Alt+E)
   - Cocher "Common Language Runtime Exceptions"

3. **Reproduire** : Envoyer la requête

4. **Debugger s'arrête sur l'exception** :
   - Lire le message
   - Inspecter la stack trace
   - Corriger le bug

---

## 🛠️ Collection Postman Complète

### Créer une Collection

**Import cette collection** :

```json
{
  "info": {
    "name": "KBA Microservices",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "variable": [
    {
      "key": "base_url",
      "value": "http://localhost:5000"
    },
    {
      "key": "identity_url",
      "value": "http://localhost:5001"
    },
    {
      "key": "product_url",
      "value": "http://localhost:5002"
    },
    {
      "key": "permission_url",
      "value": "http://localhost:5004"
    },
    {
      "key": "token",
      "value": ""
    },
    {
      "key": "user_id",
      "value": ""
    }
  ],
  "item": [
    {
      "name": "Auth",
      "item": [
        {
          "name": "Register",
          "request": {
            "method": "POST",
            "url": "{{identity_url}}/api/auth/register",
            "body": {
              "mode": "raw",
              "raw": "{\n  \"email\": \"test@example.com\",\n  \"password\": \"Test123!\",\n  \"firstName\": \"Test\",\n  \"lastName\": \"User\"\n}"
            }
          }
        },
        {
          "name": "Login",
          "request": {
            "method": "POST",
            "url": "{{identity_url}}/api/auth/login",
            "body": {
              "mode": "raw",
              "raw": "{\n  \"email\": \"test@example.com\",\n  \"password\": \"Test123!\"\n}"
            }
          },
          "event": [
            {
              "listen": "test",
              "script": {
                "exec": [
                  "var jsonData = pm.response.json();",
                  "pm.collectionVariables.set(\"token\", jsonData.token);",
                  "pm.collectionVariables.set(\"user_id\", jsonData.userId);"
                ]
              }
            }
          ]
        }
      ]
    },
    {
      "name": "Products",
      "item": [
        {
          "name": "List Products",
          "request": {
            "method": "GET",
            "url": "{{product_url}}/api/products"
          }
        },
        {
          "name": "Search Products",
          "request": {
            "method": "GET",
            "url": "{{product_url}}/api/products/search?searchTerm=iPhone&pageSize=10"
          }
        },
        {
          "name": "Create Product (needs permission)",
          "request": {
            "method": "POST",
            "url": "{{product_url}}/api/products",
            "auth": {
              "type": "bearer",
              "bearer": [{"key": "token", "value": "{{token}}"}]
            },
            "body": {
              "mode": "raw",
              "raw": "{\n  \"name\": \"Test Product\",\n  \"sku\": \"TEST-001\",\n  \"price\": 99.99,\n  \"stock\": 10,\n  \"category\": \"Test\"\n}"
            }
          }
        }
      ]
    },
    {
      "name": "Permissions",
      "item": [
        {
          "name": "List Permissions",
          "request": {
            "method": "GET",
            "url": "{{permission_url}}/api/permissions"
          }
        },
        {
          "name": "Check Permission",
          "request": {
            "method": "POST",
            "url": "{{permission_url}}/api/permissions/check",
            "body": {
              "mode": "raw",
              "raw": "{\n  \"userId\": \"{{user_id}}\",\n  \"permissionName\": \"Products.Create\"\n}"
            }
          }
        },
        {
          "name": "Grant Permission",
          "request": {
            "method": "POST",
            "url": "{{permission_url}}/api/permissions/grant",
            "auth": {
              "type": "bearer",
              "bearer": [{"key": "token", "value": "{{token}}"}]
            },
            "body": {
              "mode": "raw",
              "raw": "{\n  \"permissionName\": \"Products.Create\",\n  \"providerName\": \"User\",\n  \"providerKey\": \"{{user_id}}\"\n}"
            }
          }
        }
      ]
    }
  ]
}
```

---

## 📝 Checklist de Debug

### Avant de Débugger

- [ ] Tous les services tournent (`start-microservices.ps1`)
- [ ] Health checks OK pour tous
- [ ] Token JWT valide et non expiré
- [ ] URLs correctes dans `appsettings.json`

### Pendant le Debug

- [ ] Breakpoints dans les 2 services
- [ ] Debuggers attachés aux 2 processus
- [ ] Correlation ID activé
- [ ] Logs visibles (Output Window)

### Si Problème

- [ ] Vérifier les logs des 2 services
- [ ] Vérifier le Correlation ID dans Seq
- [ ] Tester chaque service séparément
- [ ] Vérifier la configuration réseau (ports, firewalls)

---

## 🎉 Résumé

### Tester avec Postman
✅ Démarrer services → Obtenir token → Utiliser token dans headers

### Communication Entre Services
✅ HttpClient configuré → Appel HTTP → Traiter réponse

### Debugging Multi-Services
✅ Attach to Process (2 debuggers) → Breakpoints dans les 2 → Suivre le flow

### 2 Projets Différents ?
✅ **Oui**, mais on peut debugger les 2 **en même temps** !

---

**La clé** : Les **Correlation IDs** et les **logs structurés** rendent le debugging multi-services **beaucoup plus facile** ! 🚀
