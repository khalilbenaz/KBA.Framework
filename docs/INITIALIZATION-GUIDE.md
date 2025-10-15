# Guide d'initialisation - KBA Framework

Ce guide vous explique comment initialiser le système KBA Framework pour la première utilisation.

## 📋 Table des matières

- [Vue d'ensemble](#vue-densemble)
- [Prérequis](#prérequis)
- [Méthode 1 : Via Swagger UI](#méthode-1--via-swagger-ui)
- [Méthode 2 : Via ReDoc](#méthode-2--via-redoc)
- [Méthode 3 : Via cURL](#méthode-3--via-curl)
- [Méthode 4 : Via PowerShell](#méthode-4--via-powershell)
- [Vérification de l'initialisation](#vérification-de-linitialisation)
- [Connexion après initialisation](#connexion-après-initialisation)
- [Dépannage](#dépannage)

## Vue d'ensemble

Lors de la première utilisation du KBA Framework, vous devez créer le **premier utilisateur administrateur**. Cet utilisateur aura tous les privilèges nécessaires pour gérer le système.

## Prérequis

1. L'application doit être en cours d'exécution
2. La base de données doit être créée et migrée
3. Aucun utilisateur ne doit exister dans la base de données

## Méthode 1 : Via Swagger UI

### Étape 1 : Accéder à Swagger UI

Ouvrez votre navigateur et accédez à :
```
http://localhost:5220/swagger
```

### Étape 2 : Vérifier le statut

1. Trouvez la section **Initialization**
2. Cliquez sur `GET /api/init/status`
3. Cliquez sur **"Try it out"**
4. Cliquez sur **"Execute"**
5. Vérifiez que `needsInitialization` est `true`

### Étape 3 : Créer le premier administrateur

1. Dans la section **Initialization**, trouvez `POST /api/init/first-admin`
2. Cliquez sur **"Try it out"**
3. Remplissez le JSON avec vos informations :

```json
{
  "userName": "admin",
  "email": "admin@kba-framework.com",
  "password": "Admin@123456",
  "firstName": "Admin",
  "lastName": "System",
  "phoneNumber": "+33612345678"
}
```

4. Cliquez sur **"Execute"**
5. Vous devriez recevoir une réponse **200 OK** avec les détails de l'utilisateur créé

### Étape 4 : Se connecter

1. Trouvez la section **Authentication**
2. Cliquez sur `POST /api/auth/login`
3. Cliquez sur **"Try it out"**
4. Entrez vos identifiants :

```json
{
  "userName": "admin",
  "password": "Admin@123456"
}
```

5. Cliquez sur **"Execute"**
6. Copiez le `token` de la réponse

### Étape 5 : Autoriser les requêtes

1. Cliquez sur le bouton **"Authorize"** en haut de la page
2. Entrez : `Bearer VOTRE_TOKEN` (remplacez VOTRE_TOKEN par le token copié)
3. Cliquez sur **"Authorize"**
4. Fermez le dialogue

✅ Vous pouvez maintenant tester tous les endpoints protégés !

## Méthode 2 : Via ReDoc

### Accéder à la documentation

1. Ouvrez votre navigateur et accédez à :
```
http://localhost:5220/api-docs
```

2. La documentation ReDoc affiche tous les endpoints disponibles
3. Pour tester les endpoints, utilisez l'un des boutons **"Authorize"** dans l'interface
4. Suivez les mêmes étapes que pour Swagger UI pour l'authentification

### Avantages de ReDoc

- Interface plus épurée et moderne
- Meilleure présentation de la documentation
- Support natif de l'authentification Bearer Token
- Possibilité de tester directement depuis l'interface

## Méthode 3 : Via cURL

### Étape 1 : Vérifier le statut

```bash
curl -X GET "http://localhost:5220/api/init/status" -H "accept: application/json"
```

### Étape 2 : Créer le premier administrateur

```bash
curl -X POST "http://localhost:5220/api/init/first-admin" \
  -H "Content-Type: application/json" \
  -d '{
    "userName": "admin",
    "email": "admin@kba-framework.com",
    "password": "Admin@123456",
    "firstName": "Admin",
    "lastName": "System",
    "phoneNumber": "+33612345678"
  }'
```

### Étape 3 : Se connecter

```bash
curl -X POST "http://localhost:5220/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "userName": "admin",
    "password": "Admin@123456"
  }'
```

Copiez le token de la réponse.

### Étape 4 : Utiliser le token

```bash
curl -X GET "http://localhost:5220/api/users" \
  -H "Authorization: Bearer VOTRE_TOKEN" \
  -H "accept: application/json"
```

## Méthode 4 : Via PowerShell

### Script d'initialisation complet

```powershell
# Configuration
$baseUrl = "http://localhost:5220"
$adminUser = @{
    userName = "admin"
    email = "admin@kba-framework.com"
    password = "Admin@123456"
    firstName = "Admin"
    lastName = "System"
    phoneNumber = "+33612345678"
}

# 1. Vérifier le statut
Write-Host "Vérification du statut d'initialisation..." -ForegroundColor Yellow
$statusResponse = Invoke-RestMethod -Uri "$baseUrl/api/init/status" -Method Get
Write-Host "Status: $($statusResponse.message)" -ForegroundColor Green

if ($statusResponse.needsInitialization) {
    # 2. Créer le premier administrateur
    Write-Host "`nCréation du premier administrateur..." -ForegroundColor Yellow
    $createResponse = Invoke-RestMethod -Uri "$baseUrl/api/init/first-admin" `
        -Method Post `
        -ContentType "application/json" `
        -Body ($adminUser | ConvertTo-Json)
    Write-Host "Utilisateur créé: $($createResponse.user.userName)" -ForegroundColor Green

    # 3. Se connecter
    Write-Host "`nConnexion..." -ForegroundColor Yellow
    $loginData = @{
        userName = $adminUser.userName
        password = $adminUser.password
    }
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" `
        -Method Post `
        -ContentType "application/json" `
        -Body ($loginData | ConvertTo-Json)
    
    Write-Host "Token obtenu!" -ForegroundColor Green
    Write-Host "Token: $($loginResponse.token)" -ForegroundColor Cyan
    
    # 4. Tester avec le token
    Write-Host "`nTest de l'authentification..." -ForegroundColor Yellow
    $headers = @{
        "Authorization" = "Bearer $($loginResponse.token)"
    }
    $usersResponse = Invoke-RestMethod -Uri "$baseUrl/api/users" `
        -Method Get `
        -Headers $headers
    
    Write-Host "Nombre d'utilisateurs: $($usersResponse.Count)" -ForegroundColor Green
    Write-Host "`n✅ Initialisation terminée avec succès!" -ForegroundColor Green
} else {
    Write-Host "`n⚠️  Le système est déjà initialisé." -ForegroundColor Yellow
}
```

Sauvegardez ce script dans `init.ps1` et exécutez-le :

```powershell
.\init.ps1
```

## Vérification de l'initialisation

À tout moment, vous pouvez vérifier le statut d'initialisation :

### Via la page d'accueil
```
http://localhost:5220
```

### Via l'endpoint de statut
```
http://localhost:5220/api/init/status
```

Réponse attendue après initialisation :
```json
{
  "needsInitialization": false,
  "userCount": 1,
  "message": "Le système est déjà initialisé."
}
```

## Connexion après initialisation

Une fois le premier administrateur créé, vous ne pouvez plus utiliser l'endpoint `/api/init/first-admin`.

Pour vous connecter :

1. Utilisez `/api/auth/login` avec vos identifiants
2. Récupérez le token JWT
3. Utilisez le token dans l'en-tête Authorization : `Bearer VOTRE_TOKEN`

## Dépannage

### Erreur : "Le système est déjà initialisé"

**Cause** : Un utilisateur existe déjà dans la base de données.

**Solutions** :
- Si vous connaissez les identifiants, connectez-vous via `/api/auth/login`
- Sinon, supprimez la base de données et recréez-la :

```bash
dotnet ef database drop --project src/KBA.Framework.Infrastructure --startup-project src/KBA.Framework.Api --force
dotnet ef database update --project src/KBA.Framework.Infrastructure --startup-project src/KBA.Framework.Api
```

### Erreur : "Validation failed"

**Cause** : Le mot de passe ne respecte pas les critères de sécurité.

**Solution** : Assurez-vous que le mot de passe :
- Contient au moins 8 caractères
- Contient au moins une majuscule
- Contient au moins une minuscule
- Contient au moins un chiffre
- Contient au moins un caractère spécial (@, #, $, etc.)

### Erreur : Connection refusée

**Cause** : L'application n'est pas en cours d'exécution.

**Solution** : Démarrez l'application :

```bash
dotnet run --project src/KBA.Framework.Api
```

### Erreur : 401 Unauthorized

**Cause** : Token expiré ou invalide.

**Solution** : 
- Reconnectez-vous via `/api/auth/login`
- Vérifiez que vous utilisez le format correct : `Bearer VOTRE_TOKEN`

## Recommandations de sécurité

### ⚠️ Important pour la production

1. **Changez immédiatement le mot de passe par défaut**
2. **Utilisez un email valide** pour la récupération de compte
3. **Configurez HTTPS** pour toutes les communications
4. **Limitez l'accès** à l'endpoint d'initialisation en production
5. **Activez la journalisation** des tentatives de connexion
6. **Configurez la rotation des tokens** JWT

### Bonnes pratiques

- Créez plusieurs utilisateurs avec des rôles différents
- N'utilisez pas le compte admin pour les opérations quotidiennes
- Activez l'authentification à deux facteurs (2FA)
- Surveillez les logs d'audit pour détecter les activités suspectes

## Prochaines étapes

Après l'initialisation réussie :

1. ✅ Créez d'autres utilisateurs via `/api/users`
2. ✅ Configurez les rôles et permissions
3. ✅ Créez des tenants pour le multi-tenancy
4. ✅ Explorez l'API via Swagger ou ReDoc
5. ✅ Consultez la documentation complète

## Ressources supplémentaires

- [README.md](../README.md) - Documentation principale
- [API Documentation](http://localhost:5220/api-docs) - Documentation ReDoc
- [Swagger UI](http://localhost:5220/swagger) - Interface de test

---

**KBA Framework** - Production-Ready Clean Architecture pour .NET 8
