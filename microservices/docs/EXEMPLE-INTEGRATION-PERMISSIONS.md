# 🔐 Exemple d'Intégration des Permissions

## 🎯 Objectif

Montrer comment **Product Service** vérifie les permissions via **Permission Service** avant d'autoriser une action.

---

## 📁 Fichiers Modifiés/Créés

### 1. PermissionServiceClient.cs (CRÉÉ)

**Fichier** : `KBA.ProductService/Services/PermissionServiceClient.cs`

✅ **Déjà créé** - Client HTTP pour communiquer avec Permission Service

**Features** :
- ✅ Appel HTTP au Permission Service
- ✅ Propagation du Correlation ID
- ✅ Gestion d'erreurs (fail-safe)
- ✅ Logging structuré

### 2. Program.cs (MODIFIÉ)

**Fichier** : `KBA.ProductService/Program.cs`

✅ **Déjà modifié** - Enregistrement du client

**Ajouts** :
```csharp
builder.Services.AddScoped<IPermissionServiceClient, PermissionServiceClient>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient("PermissionService", ...);
```

### 3. ProductsController.cs (À MODIFIER)

**Fichier** : `KBA.ProductService/Controllers/ProductsController.cs`

**À faire** : Ajouter la vérification de permissions

---

## 🔧 Implémentation dans ProductsController

### Étape 1 : Injecter le Service Client

```csharp
using KBA.Framework.Domain.Common;
using KBA.ProductService.DTOs;
using KBA.ProductService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;  // ← AJOUTER

namespace KBA.ProductService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductServiceLogic _productService;
    private readonly IPermissionServiceClient _permissionService;  // ← AJOUTER
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductServiceLogic productService,
        IPermissionServiceClient permissionService,  // ← AJOUTER
        ILogger<ProductsController> logger)
    {
        _productService = productService;
        _permissionService = permissionService;  // ← AJOUTER
        _logger = logger;
    }

    // ... autres méthodes
}
```

### Étape 2 : Modifier la Méthode Create

**Avant** (sans vérification de permission) :

```csharp
[HttpPost]
[Authorize]
[ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
{
    try
    {
        var product = await _productService.CreateAsync(dto);
        _logger.LogInformation("Produit créé: {ProductId}", product.Id);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}
```

**Après** (avec vérification de permission) :

```csharp
[HttpPost]
[Authorize]
[ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]  // ← AJOUTER
public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
{
    // ========== NOUVEAU CODE ==========
    
    // 1. Récupérer l'ID utilisateur du token JWT
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
    {
        _logger.LogWarning("User ID not found in JWT token");
        return Unauthorized(new { message = "User ID not found in token" });
    }

    // 2. Vérifier la permission via Permission Service
    var hasPermission = await _permissionService.CheckPermissionAsync(
        userId,
        "Products.Create"  // ← Nom de la permission
    );

    if (!hasPermission)
    {
        _logger.LogWarning(
            "User {UserId} attempted to create product without permission",
            userId
        );
        return Forbid(); // 403 Forbidden
    }
    
    // ========== FIN NOUVEAU CODE ==========

    // 3. Créer le produit (code existant)
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
```

### Étape 3 : Appliquer aux Autres Méthodes

#### Update

```csharp
[HttpPut("{id}")]
[Authorize]
public async Task<ActionResult<ProductDto>> Update(Guid id, [FromBody] UpdateProductDto dto)
{
    // Récupérer userId
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!Guid.TryParse(userIdClaim, out var userId))
        return Unauthorized();

    // Vérifier permission
    var hasPermission = await _permissionService.CheckPermissionAsync(
        userId,
        "Products.Edit"  // ← Permission différente
    );

    if (!hasPermission)
        return Forbid();

    // Update logic...
    try
    {
        var product = await _productService.UpdateAsync(id, dto);
        _logger.LogInformation("Product updated: {ProductId} by user {UserId}", id, userId);
        return Ok(product);
    }
    catch (KeyNotFoundException)
    {
        return NotFound();
    }
}
```

#### Delete

```csharp
[HttpDelete("{id}")]
[Authorize]
public async Task<ActionResult> Delete(Guid id)
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!Guid.TryParse(userIdClaim, out var userId))
        return Unauthorized();

    var hasPermission = await _permissionService.CheckPermissionAsync(
        userId,
        "Products.Delete"
    );

    if (!hasPermission)
        return Forbid();

    try
    {
        await _productService.DeleteAsync(id);
        _logger.LogInformation("Product deleted: {ProductId} by user {UserId}", id, userId);
        return NoContent();
    }
    catch (KeyNotFoundException)
    {
        return NotFound();
    }
}
```

#### UpdateStock

```csharp
[HttpPatch("{id}/stock")]
[Authorize]
public async Task<ActionResult<bool>> UpdateStock(Guid id, [FromBody] int quantity)
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (!Guid.TryParse(userIdClaim, out var userId))
        return Unauthorized();

    var hasPermission = await _permissionService.CheckPermissionAsync(
        userId,
        "Products.ManageStock"  // ← Permission spécifique pour le stock
    );

    if (!hasPermission)
        return Forbid();

    try
    {
        var result = await _productService.UpdateStockAsync(id, quantity);
        _logger.LogInformation(
            "Stock updated for product {ProductId} by user {UserId} -> {Quantity}",
            id,
            userId,
            quantity
        );
        return Ok(result);
    }
    catch (KeyNotFoundException)
    {
        return NotFound();
    }
}
```

---

## 🧪 Tester avec Postman

### Scénario 1 : Créer un Produit SANS Permission

#### 1. Se connecter

```http
POST http://localhost:5001/api/auth/login
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "Test123!"
}
```

**Copier le `token` et `userId`**

#### 2. Essayer de créer un produit

```http
POST http://localhost:5002/api/products
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Test Product",
  "sku": "TEST-001",
  "price": 99.99,
  "stock": 10,
  "category": "Test"
}
```

**Résultat attendu** : `403 Forbidden` ❌

**Logs dans Product Service** :
```
[ProductService] Checking permission Products.Create for user {userId}
[ProductService] User {userId} attempted to create product without permission
```

**Logs dans Permission Service** :
```
[PermissionService] Checking permission Products.Create for user {userId}
[PermissionService] Permission NOT granted (no grant found)
```

### Scénario 2 : Accorder la Permission

```http
POST http://localhost:5004/api/permissions/grant
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "permissionName": "Products.Create",
  "providerName": "User",
  "providerKey": "{userId}",
  "tenantId": null
}
```

**Résultat** : `200 OK` - Permission accordée ✅

### Scénario 3 : Réessayer de Créer

```http
POST http://localhost:5002/api/products
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "Test Product",
  "sku": "TEST-001",
  "price": 99.99,
  "stock": 10,
  "category": "Test"
}
```

**Résultat attendu** : `201 Created` ✅

**Logs** :
```
[ProductService] Checking permission Products.Create for user {userId}
[PermissionService] Checking permission Products.Create for user {userId}
[PermissionService] Permission granted via User provider
[ProductService] Permission check result: True
[ProductService] Product created: {productId} by user {userId}
```

---

## 🐛 Debugging Pas à Pas

### 1. Démarrer en Mode Debug

**Option A** : Visual Studio Multiple Startup Projects
- Product Service
- Permission Service

**Option B** : Attach to Process
```
Attach → dotnet.exe (ProductService)
Attach → dotnet.exe (PermissionService)
```

### 2. Mettre les Breakpoints

**Product Service** (`ProductsController.cs`) :
```csharp
[HttpPost]
public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // ← BP 1
    
    var hasPermission = await _permissionService.CheckPermissionAsync(...); // ← BP 2
    
    if (!hasPermission) // ← BP 3
    {
        return Forbid();
    }
    
    var product = await _productService.CreateAsync(dto); // ← BP 4
}
```

**Product Service** (`PermissionServiceClient.cs`) :
```csharp
public async Task<bool> CheckPermissionAsync(...)
{
    var request = new CheckPermissionRequest { ... }; // ← BP 5
    
    var response = await client.PostAsJsonAsync(...); // ← BP 6
    
    var result = await response.Content.ReadFromJsonAsync<...>(); // ← BP 7
    
    return result?.IsGranted ?? false; // ← BP 8
}
```

**Permission Service** (`PermissionsController.cs`) :
```csharp
[HttpPost("check")]
public async Task<ActionResult<PermissionCheckResult>> CheckPermission(...)
{
    var result = await _permissionService.CheckPermissionAsync(dto); // ← BP 9
    return Ok(result); // ← BP 10
}
```

### 3. Envoyer la Requête Postman

```http
POST http://localhost:5002/api/products
Authorization: Bearer {token}
...
```

### 4. Suivre le Flow

**Debugger s'arrête** :

1. ✅ **BP 1** : Extraction userId du token
   - Inspecter : `userIdClaim`, `userId`
   
2. ✅ **BP 2** : Appel au Permission Service
   - F11 (Step Into) → Entre dans `CheckPermissionAsync`
   
3. ✅ **BP 5** : Construction de la requête
   - Inspecter : `request.UserId`, `request.PermissionName`
   
4. ✅ **BP 6** : Envoi HTTP
   - F10 (Step Over) → Attend réponse
   
5. 🔄 **HTTP Call en cours**

6. ✅ **BP 9** : Permission Service reçoit la requête
   - Inspecter : `dto.UserId`, `dto.PermissionName`
   - F11 → Entre dans la logique de vérification
   
7. ✅ **BP 10** : Retourne le résultat
   - Inspecter : `result.IsGranted`
   - F5 (Continue)
   
8. 🔄 **HTTP Response**

9. ✅ **BP 7** : Product Service reçoit la réponse
   - Inspecter : `response.StatusCode`, `result`
   
10. ✅ **BP 8** : Retourne IsGranted
    - Inspecter : `result.IsGranted`
    - F5
    
11. ✅ **BP 2** : Retour dans Create
    - Inspecter : `hasPermission` (true/false)
    
12. ✅ **BP 3** : Vérification
    - Si `false` → Entre dans le `if` et retourne `Forbid()`
    - Si `true` → Continue
    
13. ✅ **BP 4** : Création du produit
    - F11 → Entre dans `CreateAsync`

### 5. Analyser

**Call Stack** :
```
ProductsController.Create()
  ↓
PermissionServiceClient.CheckPermissionAsync()
  ↓
HttpClient.PostAsJsonAsync()
  ↓
[HTTP to Permission Service]
  ↓
PermissionsController.CheckPermission()
  ↓
PermissionServiceLogic.CheckPermissionAsync()
```

**Output Window** :
```
[ProductService] Checking permission Products.Create for user abc-123
[PermissionService] Checking permission Products.Create for user abc-123
[PermissionService] Permission granted via User provider
[ProductService] Permission check result: True
[ProductService] Product created: xyz-789 by user abc-123
```

---

## ✅ Checklist d'Implémentation

### Fichiers à Créer/Modifier

- [x] `PermissionServiceClient.cs` - ✅ Créé
- [x] `Program.cs` - ✅ Modifié (HttpClient configuré)
- [ ] `ProductsController.cs` - ⏳ À modifier (ajouter vérifications)

### Permissions à Définir

- [ ] `Products.View` - Voir les produits (déjà existe)
- [ ] `Products.Create` - Créer un produit (déjà existe)
- [ ] `Products.Edit` - Modifier un produit (déjà existe)
- [ ] `Products.Delete` - Supprimer un produit (déjà existe)
- [ ] `Products.ManageStock` - Gérer le stock (déjà existe)

✅ **Toutes les permissions sont déjà créées** dans Permission Service !

### Tests

- [ ] Tester sans permission → 403 Forbidden
- [ ] Accorder permission
- [ ] Tester avec permission → 201 Created
- [ ] Tester avec mauvais token → 401 Unauthorized
- [ ] Tester sans token → 401 Unauthorized

---

## 🎯 Prochaines Étapes

### 1. Appliquer aux Autres Services

**Identity Service** :
- `Users.Create`, `Users.Edit`, `Users.Delete`, `Users.View`

**Tenant Service** :
- `Tenants.Create`, `Tenants.Edit`, `Tenants.Delete`, `Tenants.View`

### 2. Créer un Attribut Personnalisé

**Plus élégant** :

```csharp
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequirePermissionAttribute : TypeFilterAttribute
{
    public RequirePermissionAttribute(string permission) 
        : base(typeof(PermissionFilter))
    {
        Arguments = new object[] { permission };
    }
}

// Utilisation
[HttpPost]
[Authorize]
[RequirePermission("Products.Create")]  // ← Plus simple !
public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
{
    // Pas besoin de vérifier manuellement
    var product = await _productService.CreateAsync(dto);
    return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
}
```

### 3. Tests Unitaires

```csharp
[Fact]
public async Task Create_WithoutPermission_ReturnsForbidden()
{
    // Arrange
    var mockPermissionService = new Mock<IPermissionServiceClient>();
    mockPermissionService
        .Setup(x => x.CheckPermissionAsync(It.IsAny<Guid>(), "Products.Create", null))
        .ReturnsAsync(false);
    
    var controller = new ProductsController(..., mockPermissionService.Object, ...);
    
    // Act
    var result = await controller.Create(new CreateProductDto { ... });
    
    // Assert
    Assert.IsType<ForbidResult>(result.Result);
}
```

---

## 🎉 Résumé

✅ **PermissionServiceClient créé** - Communication HTTP  
✅ **Program.cs configuré** - HttpClient enregistré  
✅ **Guide complet** - Implémentation pas à pas  
✅ **Exemple de debug** - Flow complet  
✅ **Tests Postman** - Scénarios réels  

**Maintenant vous pouvez** :
- Tester avec Postman
- Débugger les 2 services ensemble
- Tracer les appels avec Correlation ID

**Communication entre services = Maîtrisée ! 🚀**
