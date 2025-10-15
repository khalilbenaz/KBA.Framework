# Guide d'initialisation - KBA Framework

Guide complet pour initialiser votre système KBA Framework et créer le premier utilisateur administrateur.

## 📋 Table des matières

- [Vue d'ensemble](#vue-densemble)
- [Méthode rapide (Recommandée)](#méthode-rapide-recommandée)
- [Méthode 1 : Via Swagger UI](#méthode-1--via-swagger-ui)
- [Méthode 2 : Via ReDoc](#méthode-2--via-redoc)
- [Méthode 3 : Via cURL](#méthode-3--via-curl)
- [Vérification et connexion](#vérification-et-connexion)
- [Dépannage](#dépannage)

## Vue d'ensemble

Lors de la première utilisation, vous devez créer le **premier utilisateur administrateur**. 

**Important** : 
- ✅ Une seule chaîne de connexion dans `appsettings.json` (section `ConnectionStrings`)
- ✅ Tags personnalisés dans Swagger (Authentication, Users, Products, Initialization)
- ✅ Page d'accueil disponible sur `http://localhost:5220`
- ✅ Test JWT fonctionnel dans Swagger et ReDoc

## Méthode rapide (Recommandée)

### Via le script PowerShell

```powershell
.\init-first-admin.ps1
```

Le script vous guide automatiquement :
1. Vérifie la connexion à l'API
2. Vérifie le statut d'initialisation
3. Demande vos informations
4. Crée l'utilisateur
5. Teste la connexion
6. Affiche un résumé

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


## Vérification et connexion

### Vérifier le statut d'initialisation

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

### Se connecter après initialisation

**Via Swagger/ReDoc :**
1. Endpoint `/api/auth/login` avec vos identifiants
2. Copiez le token JWT reçu
3. Cliquez sur "Authorize" en haut de la page
4. Entrez : `Bearer VOTRE_TOKEN`

**Via cURL :**
```bash
# Obtenir le token
TOKEN=$(curl -X POST http://localhost:5220/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"Admin@123456"}' | jq -r '.token')

# Utiliser le token
curl http://localhost:5220/api/users \
  -H "Authorization: Bearer $TOKEN"
```

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

## 🎯 Prochaines étapes

Après l'initialisation :

1. **Changez le mot de passe par défaut** (sécurité)
2. **Créez d'autres utilisateurs** via `/api/users`
3. **Explorez l'API** via Swagger (`/swagger`) ou ReDoc (`/api-docs`)
4. **Consultez le README** pour ajouter vos propres entités

## 📚 Ressources

- **Page d'accueil** : http://localhost:5220
- **Swagger UI** : http://localhost:5220/swagger (test interactif)
- **ReDoc** : http://localhost:5220/api-docs (documentation)
- **README** : [README.md](../README.md)

---

**KBA Framework** - Production-Ready Clean Architecture pour .NET 8
