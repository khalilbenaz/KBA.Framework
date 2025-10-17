# Guide de Débogage - Microservices

## 🎯 Méthodes de Débogage

### 1. Visual Studio - Multi-Startup (Recommandé)

#### Configuration

1. **Ouvrir** `KBA.Microservices.sln` dans Visual Studio
2. **Clic droit** sur la solution → **Configure Startup Projects**
3. **Sélectionner** "Multiple startup projects"
4. **Configurer** les projets :

| Projet | Action | Arguments |
|--------|--------|-----------|
| KBA.ApiGateway | Start | |
| KBA.IdentityService | Start | |
| KBA.ProductService | Start | |
| KBA.TenantService | Start | |

5. **Appuyer sur F5** → Tous les services démarrent simultanément !

#### Avantages
- ✅ Breakpoints dans tous les services
- ✅ Pas besoin de Docker
- ✅ Hot Reload disponible
- ✅ Debugging en temps réel

#### Configuration des Ports (launchSettings.json)

```json
// KBA.IdentityService/Properties/launchSettings.json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": false,
      "applicationUrl": "http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### 2. Visual Studio Code

#### launch.json

Créez `.vscode/launch.json` à la racine :

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
      "args": [],
      "cwd": "${workspaceFolder}/microservices/KBA.IdentityService",
      "stopAtEntry": false,
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": "http://localhost:5001"
      },
      "sourceFileMap": {
        "/Views": "${workspaceFolder}/Views"
      }
    },
    {
      "name": "Product Service",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/microservices/KBA.ProductService/bin/Debug/net8.0/KBA.ProductService.dll",
      "args": [],
      "cwd": "${workspaceFolder}/microservices/KBA.ProductService",
      "stopAtEntry": false,
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": "http://localhost:5002"
      }
    },
    {
      "name": "Tenant Service",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/microservices/KBA.TenantService/bin/Debug/net8.0/KBA.TenantService.dll",
      "args": [],
      "cwd": "${workspaceFolder}/microservices/KBA.TenantService",
      "stopAtEntry": false,
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": "http://localhost:5003"
      }
    },
    {
      "name": "API Gateway",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/microservices/KBA.ApiGateway/bin/Debug/net8.0/KBA.ApiGateway.dll",
      "args": [],
      "cwd": "${workspaceFolder}/microservices/KBA.ApiGateway",
      "stopAtEntry": false,
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": "http://localhost:5000"
      }
    }
  ],
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

#### tasks.json

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "build",
      "command": "dotnet",
      "type": "process",
      "args": [
        "build",
        "${workspaceFolder}/microservices/KBA.Microservices.sln"
      ],
      "problemMatcher": "$msCompile"
    }
  ]
}
```

#### Utilisation

1. **F5** → Sélectionner "All Microservices"
2. Tous les services démarrent avec le debugger attaché

### 3. Attach to Process

#### Scénario
Vous avez démarré les services avec `start-microservices.ps1` et voulez attacher le debugger.

#### Étapes

1. **Démarrer** les services normalement
2. **Visual Studio** → Debug → Attach to Process (Ctrl+Alt+P)
3. **Filtrer** par `dotnet.exe`
4. **Sélectionner** les processus des microservices
5. **Attach**

#### Identifier les processus

```powershell
# PowerShell : Lister les processus dotnet avec leurs ports
Get-NetTCPConnection | Where-Object {$_.LocalPort -in 5000,5001,5002,5003} | 
  Select-Object LocalPort, @{Name="ProcessName";Expression={(Get-Process -Id $_.OwningProcess).ProcessName}}
```

### 4. Remote Debugging (Docker)

#### Dockerfile.debug

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 4000

# Installer vsdbg (Visual Studio Debugger)
RUN apt-get update && apt-get install -y unzip && \
    curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /vsdbg

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet build "KBA.IdentityService/KBA.IdentityService.csproj" -c Debug

FROM build AS publish
RUN dotnet publish "KBA.IdentityService/KBA.IdentityService.csproj" -c Debug -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Démarrer avec le debugger
ENTRYPOINT ["/vsdbg/vsdbg", "--interpreter=vscode", "--connection=0.0.0.0:4000"]
```

#### Attacher depuis Visual Studio

1. Debug → Attach to Process
2. Connection type: Docker (Linux Container)
3. Sélectionner le container

## 🔍 Techniques de Débogage

### 1. Breakpoints Conditionnels

```csharp
// Arrêter uniquement si userId == specific value
public async Task<UserDto> GetUserAsync(Guid userId)
{
    // Breakpoint conditionnel : userId == Guid.Parse("...")
    var user = await _context.Users.FindAsync(userId);
    return user;
}
```

### 2. Logging Détaillé

```csharp
public async Task<ProductDto> CreateAsync(CreateProductDto dto)
{
    _logger.LogDebug("Début création produit: {@ProductDto}", dto);
    
    try
    {
        var product = new Product(...);
        await _context.SaveChangesAsync();
        
        _logger.LogInformation(
            "Produit créé avec succès: {ProductId} par {UserId}",
            product.Id,
            _currentUser.UserId
        );
        
        return MapToDto(product);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Erreur lors de la création du produit: {@ProductDto}", dto);
        throw;
    }
}
```

### 3. Watch Window

Variables à surveiller :
- `HttpContext.User.Claims` - Claims JWT
- `_context.ChangeTracker.Entries()` - Changements EF Core
- `HttpContext.Request.Headers` - Headers de la requête

### 4. Immediate Window

```csharp
// Pendant le debug, dans la fenêtre Immediate :
? _context.Users.Count()
? HttpContext.Request.Headers["Authorization"]
? JsonSerializer.Serialize(dto)
```

## 🧪 Tester la Communication Inter-Services

### Tester depuis l'Identity Service vers Product Service

```csharp
// Dans IdentityService/Program.cs
builder.Services.AddHttpClient("ProductService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5002");
});

// Dans un controller
public class TestController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    [HttpGet("test-product-service")]
    public async Task<IActionResult> TestProductService()
    {
        var client = _httpClientFactory.CreateClient("ProductService");
        var response = await client.GetAsync("/api/products");
        
        return Ok(new
        {
            StatusCode = response.StatusCode,
            Content = await response.Content.ReadAsStringAsync()
        });
    }
}
```

### Breakpoint sur les deux services

1. **Breakpoint** dans Identity Service sur l'appel HTTP
2. **Breakpoint** dans Product Service sur le controller
3. **F5** → Le debugger s'arrête dans les deux services !

## 📊 Debugging par Scénario

### Scénario 1 : Problème d'Authentification

#### Symptôme
401 Unauthorized sur un endpoint protégé

#### Debug

1. **Breakpoint** dans `JwtTokenService.GenerateToken()`
2. Vérifier le contenu du token généré
3. **Breakpoint** dans le middleware JWT du service appelé
4. Vérifier `HttpContext.User.Claims`

```csharp
// Middleware de logging
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var claims = string.Join(", ", context.User.Claims.Select(c => $"{c.Type}={c.Value}"));
        Log.Debug("User authenticated: {Claims}", claims);
    }
    else
    {
        Log.Warning("User not authenticated for {Path}", context.Request.Path);
    }
    
    await next();
});
```

### Scénario 2 : Communication Inter-Services Échouée

#### Symptôme
Timeout ou erreur de connexion

#### Debug

1. **Vérifier** que tous les services sont démarrés
```powershell
netstat -an | findstr "5000 5001 5002 5003"
```

2. **Tester** manuellement
```powershell
curl http://localhost:5001/health
curl http://localhost:5002/health
```

3. **Breakpoint** sur les appels HTTP
```csharp
var response = await _httpClient.GetAsync($"http://localhost:5001/api/users/{userId}");
// Breakpoint ici
if (!response.IsSuccessStatusCode)
{
    var error = await response.Content.ReadAsStringAsync();
    _logger.LogError("Failed to get user: {Error}", error);
}
```

### Scénario 3 : Problème de Base de Données

#### Symptôme
SqlException ou timeout

#### Debug

1. **Activer** le logging SQL
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

2. **Breakpoint** avant SaveChangesAsync
```csharp
var product = new Product(...);
_context.Products.Add(product);

// Inspecter les changements
var entries = _context.ChangeTracker.Entries();
// Breakpoint ici

await _context.SaveChangesAsync();
```

3. **Vérifier** la connexion
```sql
-- Dans SSMS
SELECT * FROM sys.dm_exec_sessions WHERE program_name LIKE '%Entity Framework%'
```

## 🔧 Outils Complémentaires

### 1. Postman / Swagger

**Tester les endpoints** :
1. Aller sur http://localhost:5001/swagger
2. Tester `/api/auth/login`
3. Copier le token
4. Utiliser dans les autres requêtes

### 2. Seq (Log Viewer)

```powershell
# Démarrer Seq
docker run -d --name seq -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest

# Accéder
# http://localhost:5341
```

**Filtrer les logs** :
```
Service = 'IdentityService' and Level = 'Error'
CorrelationId = 'abc-123'
```

### 3. Fiddler / Wireshark

**Capturer** le trafic HTTP entre services :
1. Démarrer Fiddler
2. Filtrer par ports 5000-5003
3. Voir les requêtes/réponses complètes

### 4. SQL Server Profiler

**Tracer** les requêtes SQL :
1. Ouvrir SQL Server Profiler
2. Se connecter à la base
3. Démarrer une trace
4. Filtrer par database `KBAFrameworkDb`

## 🎯 Checklist de Débogage

Avant de déboguer :

- [ ] Tous les services sont démarrés
- [ ] Les ports sont disponibles (5000-5003)
- [ ] La base de données est accessible
- [ ] Les appsettings.json sont corrects
- [ ] Les migrations sont appliquées
- [ ] Le JWT SecretKey est identique partout

Pendant le débogage :

- [ ] Vérifier les logs de chaque service
- [ ] Inspecter les claims du token JWT
- [ ] Vérifier les headers HTTP
- [ ] Examiner le ChangeTracker d'EF Core
- [ ] Tester les endpoints individuellement

## 💡 Tips

1. **Utilisez des CorrelationIds** pour tracer les requêtes
2. **Loggez abondamment** en développement
3. **Testez chaque service indépendamment** avant de tester via le gateway
4. **Utilisez les health checks** : `/health`
5. **Configurez Hot Reload** pour itérer rapidement

## 🆘 Problèmes Courants

### Port déjà utilisé
```powershell
# Trouver le processus
netstat -ano | findstr :5001

# Tuer le processus
taskkill /PID <PID> /F
```

### Service ne démarre pas
```powershell
# Vérifier les logs
Get-Content "microservices\KBA.IdentityService\logs\*.log" -Tail 50
```

### Debugger ne s'attache pas
1. Vérifier que le projet est en mode Debug
2. Supprimer bin/ et obj/
3. Rebuild la solution

---

**Bon débogage ! 🐛**
