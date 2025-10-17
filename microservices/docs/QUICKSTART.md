# 🚀 Guide de Démarrage Rapide - Architecture Microservices

## ⚡ Démarrage en 3 Étapes

### 1. Restaurer les packages NuGet

```powershell
cd microservices
dotnet restore KBA.Microservices.sln
```

### 2. Configurer les bases de données

Chaque microservice utilise sa propre base de données. Les migrations sont appliquées automatiquement au démarrage.

**Option A: SQL Server LocalDB (Recommandé pour développement)**

Les `appsettings.json` sont déjà configurés pour LocalDB.

**Option B: SQL Server Docker**

```powershell
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" -p 1433:1433 --name kba-sqlserver -d mcr.microsoft.com/mssql/server:2022-latest
```

Puis modifiez les `appsettings.json` de chaque service :

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=KBA[Service]Db;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true"
}
```

### 3. Démarrer les microservices

**Option A: Script automatisé (Recommandé)**

```powershell
.\start-microservices.ps1
```

**Option B: Manuel (4 terminaux)**

```powershell
# Terminal 1
cd KBA.IdentityService
dotnet run

# Terminal 2
cd KBA.ProductService
dotnet run

# Terminal 3
cd KBA.TenantService
dotnet run

# Terminal 4
cd KBA.ApiGateway
dotnet run
```

**Option C: Docker Compose**

```powershell
docker-compose up -d
```

## 🎯 Accès aux Services

| Service | URL | Description |
|---------|-----|-------------|
| **API Gateway** | http://localhost:5000 | Point d'entrée principal ⭐ |
| Identity Service | http://localhost:5001/swagger | Authentification |
| Product Service | http://localhost:5002/swagger | Gestion produits |
| Tenant Service | http://localhost:5003/swagger | Multi-tenancy |

## 🧪 Tester l'Architecture

### 1. Enregistrer un utilisateur

```bash
curl -X POST http://localhost:5000/api/identity/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "userName": "john",
    "email": "john@example.com",
    "password": "Password123!",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

### 2. Se connecter

```bash
curl -X POST http://localhost:5000/api/identity/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "userName": "john",
    "password": "Password123!"
  }'
```

Vous recevrez un token JWT :

```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "user": {
    "id": "...",
    "userName": "john",
    "email": "john@example.com"
  },
  "expiresIn": 3600
}
```

### 3. Créer un produit (avec authentification)

```bash
curl -X POST http://localhost:5000/api/products \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer VOTRE_TOKEN_ICI" \
  -d '{
    "name": "iPhone 15",
    "description": "Smartphone Apple",
    "sku": "IPHONE-15-001",
    "price": 999.99,
    "stock": 50,
    "category": "Electronics"
  }'
```

### 4. Obtenir tous les produits (public)

```bash
curl http://localhost:5000/api/products
```

## 📊 Architecture

```
Client → API Gateway (5000) → {
    ├─ Identity Service (5001) → KBAIdentityDb
    ├─ Product Service (5002)  → KBAProductDb
    └─ Tenant Service (5003)   → KBATenantDb
}
```

## 🔧 Configuration JWT

Tous les services doivent utiliser la **même clé secrète JWT** (déjà configuré) :

```json
{
  "JwtSettings": {
    "SecretKey": "VotreCleSecrete_MinimumDe32Caracteres_PourSecuriteOptimale",
    "Issuer": "KBAFramework",
    "Audience": "KBAFrameworkUsers",
    "ExpirationInMinutes": 60
  }
}
```

## 🩺 Health Checks

Vérifier l'état de chaque service :

```bash
curl http://localhost:5000/health  # API Gateway
curl http://localhost:5001/health  # Identity Service
curl http://localhost:5002/health  # Product Service
curl http://localhost:5003/health  # Tenant Service
```

## 📝 Swagger UI

- **API Gateway**: http://localhost:5000/swagger
- **Identity Service**: http://localhost:5001/swagger
- **Product Service**: http://localhost:5002/swagger
- **Tenant Service**: http://localhost:5003/swagger

## 🛠️ Commandes Utiles

### Ajouter une migration

```powershell
# Identity Service
cd KBA.IdentityService
dotnet ef migrations add NomDeLaMigration

# Product Service
cd KBA.ProductService
dotnet ef migrations add NomDeLaMigration

# Tenant Service
cd KBA.TenantService
dotnet ef migrations add NomDeLaMigration
```

### Mettre à jour la base de données

```powershell
dotnet ef database update
```

### Supprimer une base de données

```powershell
dotnet ef database drop
```

### Build tous les services

```powershell
dotnet build KBA.Microservices.sln
```

### Publier pour production

```powershell
dotnet publish KBA.Microservices.sln -c Release -o ./publish
```

## 🐛 Troubleshooting

### Erreur: Port déjà utilisé

Modifiez le port dans `appsettings.json` ou `Program.cs` :

```json
"Urls": "http://localhost:NOUVEAU_PORT"
```

### Erreur: Cannot connect to SQL Server

Vérifiez que SQL Server est démarré :

```powershell
# Pour LocalDB
sqllocaldb info
sqllocaldb start MSSQLLocalDB

# Pour Docker
docker ps | grep kba-sqlserver
```

### Erreur: Unauthorized (401)

1. Vérifiez que vous avez un token valide
2. Assurez-vous d'inclure `Authorization: Bearer TOKEN` dans le header
3. Vérifiez que tous les services utilisent la même `SecretKey` JWT

### Les services ne démarrent pas

1. Vérifiez que les ports sont disponibles (5000-5003)
2. Restaurez les packages : `dotnet restore`
3. Nettoyez et rebuilder : `dotnet clean && dotnet build`

## 📚 Documentation Complète

- [README Principal](./README.md)
- [Guide d'Architecture](./docs/ARCHITECTURE.md)
- [Guide de Déploiement](./docs/DEPLOYMENT.md)

## 💡 Tips

1. **Utilisez toujours l'API Gateway** pour accéder aux services en production
2. **Les logs** sont dans le dossier `logs/` de chaque service
3. **Swagger UI** permet de tester facilement tous les endpoints
4. **Docker Compose** simplifie le déploiement et les tests

## 🎉 C'est Prêt !

Votre architecture microservices est maintenant opérationnelle !

Accédez à **http://localhost:5000** pour commencer.
