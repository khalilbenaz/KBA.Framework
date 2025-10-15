# Guide de Test Rapide - Améliorations KBA.Framework

Ce guide vous permet de tester rapidement toutes les nouvelles fonctionnalités implémentées.

## 🚀 Démarrage

### 1. Restaurer et Compiler

```powershell
# Depuis la racine du projet
dotnet restore
dotnet build
```

### 2. Lancer l'Application

```powershell
dotnet run --project src/KBA.Framework.Api/KBA.Framework.Api.csproj
```

Attendez le message:
```
[INF] Application KBA Framework démarrée avec succès
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

---

## ✅ Test 1: Middleware de Gestion d'Erreurs

### Test d'une Erreur 404

**Requête:**
```http
GET http://localhost:5000/api/nonexistent
```

**Résultat Attendu:**
```json
{
  "statusCode": 404,
  "message": "Not Found",
  "details": "",
  "timestamp": "2025-10-15T10:30:00Z"
}
```

✅ Le middleware capture l'erreur et retourne un format standardisé.

---

## ✅ Test 2: Validation FluentValidation

### Test de Validation - Produit Invalide

**Requête:**
```http
POST http://localhost:5000/api/products
Content-Type: application/json

{
  "name": "",
  "price": -10,
  "stock": -5
}
```

**Résultat Attendu:**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": ["Le nom du produit est obligatoire."],
    "Price": ["Le prix doit être supérieur ou égal à 0."],
    "Stock": ["Le stock doit être supérieur ou égal à 0."]
  }
}
```

✅ La validation empêche les données invalides.

### Test de Validation - Produit Valide

**Requête:**
```http
POST http://localhost:5000/api/products
Content-Type: application/json

{
  "name": "Test Product",
  "description": "Description du produit",
  "price": 99.99,
  "stock": 100,
  "sku": "TEST-001",
  "category": "Test"
}
```

**Résultat Attendu:**
```json
{
  "id": "guid-generated",
  "name": "Test Product",
  "description": "Description du produit",
  "price": 99.99,
  "stock": 100,
  "sku": "TEST-001",
  "category": "Test",
  "isActive": true,
  "createdAt": "2025-10-15T10:30:00Z"
}
```

✅ Les données valides sont acceptées.

---

## ✅ Test 3: Authentification JWT

### Test de Login - Échec

**Requête:**
```http
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "userName": "wronguser",
  "password": "wrongpass"
}
```

**Résultat Attendu:**
```json
{
  "message": "Nom d'utilisateur ou mot de passe invalide."
}
```

Status: `401 Unauthorized`

✅ Les credentials invalides sont rejetés et loggés.

### Créer un Utilisateur de Test

**Requête:**
```http
POST http://localhost:5000/api/users
Content-Type: application/json

{
  "userName": "testuser",
  "email": "test@example.com",
  "password": "TestPass123!",
  "firstName": "Test",
  "lastName": "User"
}
```

### Test de Login - Succès

**Requête:**
```http
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "userName": "testuser",
  "password": "TestPass123!"
}
```

**Résultat Attendu:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ0ZXN0dXNlciIsImp0aSI6IjEyMzQ1IiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
  "refreshToken": "base64-encoded-refresh-token",
  "expiresAt": "2025-10-15T11:30:00Z",
  "userName": "testuser",
  "email": "test@example.com"
}
```

✅ Un token JWT est généré pour l'utilisateur authentifié.

### Test d'un Endpoint Protégé

**Sans Token:**
```http
GET http://localhost:5000/api/users
```

**Résultat:** `401 Unauthorized`

**Avec Token:**
```http
GET http://localhost:5000/api/users
Authorization: Bearer {votre_token_ici}
```

**Résultat:** Liste des utilisateurs (200 OK)

✅ Les endpoints sont protégés par JWT.

---

## ✅ Test 4: Swagger UI avec JWT

### Accéder à Swagger

Ouvrez votre navigateur: `http://localhost:5000/swagger`

### Tester l'Authentification dans Swagger

1. Cliquez sur le bouton **"Authorize"** 🔒 en haut à droite
2. Dans le champ **Value**, entrez: `Bearer {votre_token}`
3. Cliquez sur **"Authorize"**
4. Cliquez sur **"Close"**

Maintenant tous les endpoints protégés peuvent être testés directement depuis Swagger.

✅ Swagger est intégré avec l'authentification JWT.

---

## ✅ Test 5: Logging Serilog

### Vérifier les Logs Console

Surveillez la console lors de l'exécution. Vous devriez voir:

```
[10:30:00 INF] Démarrage de l'application KBA Framework
[10:30:01 INF] Application KBA Framework démarrée avec succès
[10:30:05 INF] HTTP GET / responded 200 in 45.2345 ms
[10:30:10 WRN] Tentative de connexion avec un nom d'utilisateur invalide: wronguser
```

✅ Les logs apparaissent en console avec le bon format.

### Vérifier les Logs Fichiers

**Emplacement:** `logs/log-YYYYMMDD.txt`

```powershell
# Voir les dernières lignes du log
Get-Content logs/log-20251015.txt -Tail 20

# Ou sur Linux/Mac
tail -f logs/log-20251015.txt
```

✅ Les logs sont écrits dans des fichiers avec rotation quotidienne.

---

## ✅ Test 6: Configuration SQL Optimisée

### Vérifier les Logs EF Core

Avec `EnableDetailedErrors` activé en développement, vous devriez voir dans les logs:

```
[10:30:05 INF] Executing DbCommand [CommandTimeout=30]
SELECT [p].[Id], [p].[Name], [p].[Price], [p].[Stock]
FROM [Products] AS [p]
WHERE [p].[IsActive] = CAST(1 AS bit)
ORDER BY [p].[Name]
```

✅ Les requêtes SQL sont loggées avec les détails.

### Tester la Résilience

**Simuler une erreur transitoire:**
- Arrêtez temporairement SQL Server
- Faites une requête
- Redémarrez SQL Server

**Résultat Attendu:**
L'application réessaie automatiquement jusqu'à 3 fois avant de lever l'erreur.

✅ Le retry logic est actif.

---

## ✅ Test 7: Optimisations EF Core

### Test de Performance - Avec vs Sans AsNoTracking

**Sans AsNoTracking (ancien code):**
```csharp
var products = await _dbSet.Where(p => p.IsActive).ToListAsync();
```

**Avec AsNoTracking (nouveau code):**
```csharp
var products = await _dbSet.AsNoTracking().Where(p => p.IsActive).ToListAsync();
```

**Mesure:**
```http
GET http://localhost:5000/api/products
```

**Attendu:** Temps de réponse réduit de 20-40% pour les listes importantes.

✅ Les requêtes en lecture seule sont optimisées.

---

## 📊 Récapitulatif des Tests

| Fonctionnalité | Test | Résultat |
|----------------|------|----------|
| Middleware Erreurs | ✅ Erreur 404 capturée | ⬜ |
| Validation | ✅ Données invalides rejetées | ⬜ |
| Validation | ✅ Données valides acceptées | ⬜ |
| JWT Login | ✅ Credentials invalides rejetés | ⬜ |
| JWT Login | ✅ Token généré pour user valide | ⬜ |
| JWT Protection | ✅ Endpoint protégé sans token → 401 | ⬜ |
| JWT Protection | ✅ Endpoint protégé avec token → 200 | ⬜ |
| Swagger JWT | ✅ Bouton Authorize visible | ⬜ |
| Logging Console | ✅ Logs affichés en console | ⬜ |
| Logging Fichiers | ✅ Fichiers créés dans logs/ | ⬜ |
| EF Logging | ✅ Requêtes SQL affichées | ⬜ |
| EF Retry | ✅ Réessai automatique actif | ⬜ |

---

## 🐛 Problèmes Courants

### 1. Erreur: "JWT SecretKey non configurée"

**Solution:** Vérifiez que `appsettings.json` contient:
```json
{
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyThatShouldBeAtLeast32CharactersLong!"
  }
}
```

### 2. Erreur: "Cannot access DbContext"

**Solution:** Vérifiez que la migration est appliquée:
```powershell
dotnet ef database update --project src/KBA.Framework.Infrastructure
```

### 3. Erreur: "FluentValidation not found"

**Solution:** Restaurez les packages:
```powershell
dotnet restore
dotnet build
```

### 4. Logs non créés

**Solution:** Vérifiez que le dossier `logs/` a les permissions d'écriture.

---

## 🔍 Tests Avancés avec PostMan

### Collection Postman

Créez une collection avec ces requêtes:

1. **Auth Login** - POST `/api/auth/login`
2. **Get Products** - GET `/api/products` avec Bearer token
3. **Create Product** - POST `/api/products` avec Bearer token
4. **Update Product** - PUT `/api/products/{id}` avec Bearer token
5. **Delete Product** - DELETE `/api/products/{id}` avec Bearer token

Utilisez des **Tests** Postman pour automatiser:

```javascript
// Dans l'onglet Tests de la requête Login
pm.test("Status code is 200", function () {
    pm.response.to.have.status(200);
});

pm.test("Token is present", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData.token).to.be.a('string');
    pm.environment.set("jwt_token", jsonData.token);
});
```

---

## 📈 Monitoring de Production

En production, surveillez:

1. **Logs d'erreurs** : Fichiers dans `logs/`
2. **Tentatives de connexion échouées** : Logs avec niveau `[WRN]`
3. **Performance EF** : Requêtes lentes (> 1 seconde)
4. **Exceptions non gérées** : Logs avec niveau `[ERR]`

---

## ✅ Conclusion

Si tous les tests passent, vous avez correctement implémenté:
- ✅ Gestion d'erreurs centralisée
- ✅ Validation robuste des données
- ✅ Sécurité JWT complète
- ✅ Logging structuré
- ✅ Configuration SQL optimisée
- ✅ Optimisations EF Core

**Prochaines étapes:** Déployer en environnement de test, puis en production avec les configurations sécurisées.

---

**Date:** 15 octobre 2025
**Version:** 1.0.0
