# ⚡ Réponse Rapide - Testing & Debugging

## ❓ Vos 3 Questions

### 1. Si je veux tester un service par Postman, comment procéder ?

**Réponse rapide** :

```powershell
# 1. Démarrer les services
.\start-microservices.ps1

# 2. Dans Postman
POST http://localhost:5001/api/auth/login
{
  "email": "test@example.com",
  "password": "Test123!"
}

# 3. Copier le token de la réponse

# 4. Tester le Product Service
GET http://localhost:5002/api/products
Authorization: Bearer {token}
```

**Guide complet** : [GUIDE-TESTING-DEBUG.md](./GUIDE-TESTING-DEBUG.md)

---

### 2. Si ce service doit consommer un autre service, comment cela va être fait ?

**Réponse rapide** :

**Product Service** appelle **Permission Service** via HTTP :

```csharp
// 1. Dans appsettings.json
{
  "ExternalServices": {
    "PermissionServiceUrl": "http://localhost:5004"
  }
}

// 2. Dans Program.cs - Configurer HttpClient
builder.Services.AddHttpClient("PermissionService", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var url = config["ExternalServices:PermissionServiceUrl"];
    client.BaseAddress = new Uri(url);
});

// 3. Créer un Service Client
public class PermissionServiceClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    public async Task<bool> CheckPermissionAsync(Guid userId, string permission)
    {
        var client = _httpClientFactory.CreateClient("PermissionService");
        
        var request = new { userId, permissionName = permission };
        var response = await client.PostAsJsonAsync("/api/permissions/check", request);
        
        var result = await response.Content.ReadFromJsonAsync<PermissionCheckResult>();
        return result?.IsGranted ?? false;
    }
}

// 4. Utiliser dans un Controller
[HttpPost]
[Authorize]
public async Task<IActionResult> Create(...)
{
    var userId = User.GetUserId();
    
    // Appel au Permission Service
    var hasPermission = await _permissionService.CheckPermissionAsync(
        userId, 
        "Products.Create"
    );
    
    if (!hasPermission)
        return Forbid(); // 403
    
    // Créer le produit
    var product = await _productService.CreateAsync(...);
    return Ok(product);
}
```

**Flow** :
```
Client (Postman)
  ↓ POST /api/products
Product Service (Port 5002)
  ↓ "Est-ce que user abc-123 a Products.Create ?"
  ↓ POST http://localhost:5004/api/permissions/check
Permission Service (Port 5004)
  ↓ Vérifie dans la base de données
  ↓ Retourne { "isGranted": true/false }
Product Service
  ↓ Si true → Crée le produit
  ↓ Si false → Retourne 403 Forbidden
Client
```

**Exemple complet** : [EXEMPLE-INTEGRATION-PERMISSIONS.md](./EXEMPLE-INTEGRATION-PERMISSIONS.md)

---

### 3. Surtout comment debugger dessus ? C'est 2 projets différents ?

**Réponse rapide** :

**OUI, ce sont 2 projets .NET différents** qui tournent dans **2 processus séparés**.

**Solution** : Debugger les 2 en même temps !

#### Méthode 1 : Multiple Startup Projects

1. Clic droit sur `KBA.Microservices.sln`
2. "Set Startup Projects..."
3. Choisir "Multiple startup projects"
4. Cocher :
   - Product Service → Start
   - Permission Service → Start
5. OK
6. **F5** → Les 2 démarrent en debug !

#### Méthode 2 : Attach to Process (Plus Flexible)

```powershell
# 1. Démarrer normalement
.\start-microservices.ps1
```

**Dans Visual Studio** :
1. `Debug` → `Attach to Process...` (Ctrl+Alt+P)
2. Filtrer : `dotnet`
3. Sélectionner :
   - `dotnet.exe` (KBA.ProductService)
   - `dotnet.exe` (KBA.PermissionService)
4. Cliquer "Attach"

#### Debugger le Flow Complet

**Mettre des breakpoints** :

**Product Service** :
```csharp
[HttpPost]
public async Task<IActionResult> Create(...)
{
    var userId = ...; // ← BREAKPOINT 1
    
    var hasPermission = await _permissionService.CheckPermissionAsync(...); // ← BP 2
    
    if (!hasPermission) // ← BP 3
        return Forbid();
}
```

**Permission Service** :
```csharp
[HttpPost("check")]
public async Task<IActionResult> CheckPermission(...)
{
    var result = await _permissionService.CheckPermissionAsync(...); // ← BP 4
    return Ok(result); // ← BP 5
}
```

**Envoyer requête Postman** → Debugger s'arrête dans cet ordre :

```
1. Product Service - BP 1 : Extraction userId
2. Product Service - BP 2 : Avant l'appel HTTP
   → F11 (Step Into) pour voir l'appel HTTP
3. [HTTP Call en cours]
4. Permission Service - BP 4 : Reçoit la requête
   → Inspecter : userId, permissionName
5. Permission Service - BP 5 : Retourne le résultat
   → Inspecter : result.IsGranted
6. [HTTP Response]
7. Product Service - BP 2 : Reçoit la réponse
   → Inspecter : hasPermission (true/false)
8. Product Service - BP 3 : Vérifie la permission
   → Si false → Entre dans le if
```

**Call Stack Window** montre :
```
ProductsController.Create()
  ↓
PermissionServiceClient.CheckPermissionAsync()
  ↓
HttpClient.PostAsJsonAsync()
  ↓
[Network: HTTP Request]
  ↓
PermissionsController.CheckPermission()
```

**Output Window** montre les logs :
```
[ProductService] Checking permission Products.Create for user abc-123
[PermissionService] Checking permission Products.Create
[PermissionService] Permission granted: True
[ProductService] Permission check result: True
[ProductService] Product created: xyz-789
```

**Correlation ID** permet de filtrer dans Seq :
```
CorrelationId = "abc-xyz-123"
→ Voir TOUS les logs des 2 services pour cette requête
```

---

## 📚 Documentation Complète

| Question | Document |
|----------|----------|
| **Tester avec Postman** | [GUIDE-TESTING-DEBUG.md](./GUIDE-TESTING-DEBUG.md) |
| **Communication entre services** | [EXEMPLE-INTEGRATION-PERMISSIONS.md](./EXEMPLE-INTEGRATION-PERMISSIONS.md) |
| **Configuration** | [CONFIGURATION-CENTRALISEE.md](./CONFIGURATION-CENTRALISEE.md) |

---

## ⚡ TL;DR

### Postman
```
1. Start services
2. Login → Get token
3. Use token in Authorization header
```

### Communication
```
Product Service → HttpClient → Permission Service
Configuré via appsettings.json
```

### Debug 2 Projets
```
Attach to Process × 2
OU
Multiple Startup Projects
→ Debugger les 2 en même temps !
```

---

## 🎯 Prochaines Étapes

1. **Lire** : [GUIDE-TESTING-DEBUG.md](./GUIDE-TESTING-DEBUG.md) (15 min)
2. **Implémenter** : [EXEMPLE-INTEGRATION-PERMISSIONS.md](./EXEMPLE-INTEGRATION-PERMISSIONS.md) (30 min)
3. **Tester** : Suivre les scénarios Postman
4. **Debugger** : Attach to Process et suivre le flow

**Vous êtes prêt ! 🚀**
