# KBA Framework - Architecture Microservices v2.0

> 📚 **Documentation** : Toute la documentation est disponible dans [docs/](./docs/) - Voir [📖 INDEX](./docs/INDEX.md)

## 🚀 Version 2.0 - Nouveautés

✅ **Product Service Enhanced** - Recherche avancée, pagination, tri  
✅ **Permission Service** - Nouveau service de gestion des permissions  
✅ **Middleware Production** - CorrelationId, GlobalException  
✅ **Tests Automatisés** - 19 tests PowerShell  
✅ **Documentation Complète** - 10 guides

👉 **Voir** : [WHATS-NEW.md](./docs/WHATS-NEW.md) | [DEMARRAGE-RAPIDE.md](./docs/DEMARRAGE-RAPIDE.md)

---

## 🏗️ Architecture

Cette solution est une architecture microservices complète avec 5 services :

### Microservices

1. **KBA.IdentityService** (Port: 5001)
   - Gestion des utilisateurs et authentification
   - Génération de tokens JWT
   - Gestion des rôles
   - Base de données: KBAFrameworkDb

2. **KBA.ProductService** (Port: 5002) ✨ Enhanced v2.0
   - Gestion des produits CRUD
   - **Recherche avancée multi-critères** 🆕
   - **Pagination et tri** 🆕
   - **Filtrage par prix, catégorie, stock** 🆕
   - **9 endpoints** (+4 nouveaux)
   - Base de données: KBAFrameworkDb

3. **KBA.TenantService** (Port: 5003)
   - Gestion multi-tenancy
   - Configuration des tenants
   - Isolation des données
   - Base de données: KBAFrameworkDb

4. **KBA.PermissionService** (Port: 5004) 🆕 NEW!
   - **Gestion centralisée des permissions**
   - **18 permissions pré-configurées**
   - **Grant/Revoke permissions**
   - **Cache Redis pour performance**
   - **11 endpoints REST**
   - Base de données: KBAFrameworkDb

5. **KBA.ApiGateway** (Port: 5000)
   - Point d'entrée unique
   - Routage vers les 4 microservices
   - Authentification centralisée JWT
   - Rate limiting

### Communication

- **Synchrone**: HTTP/REST via API Gateway
- **Asynchrone**: RabbitMQ pour les événements inter-services (optionnel)

### Schéma d'Architecture

```
┌─────────────────────────────────────────────────────────┐
│                     Clients (Web, Mobile)                │
└───────────────────────┬─────────────────────────────────┘
                        │
                        ▼
┌──────────────────────────────────────────────────────────┐
│              KBA.ApiGateway (Port 5000)                   │
│         - Routage                                         │
│         - Authentification JWT                            │
│         - Rate Limiting                                   │
└────────┬───────────────┬──────────────┬──────────────────┘
         │               │              │
         ▼               ▼              ▼
┌────────────────┐ ┌────────────┐ ┌────────────────┐
│ IdentityService│ │ ProductSvc │ │  TenantService │
│  (Port 5001)   │ │(Port 5002) │ │  (Port 5003)   │
├────────────────┤ ├────────────┤ ├────────────────┤
│ - Users        │ │ - Products │ │ - Tenants      │
│ - Auth         │ │ - CRUD     │ │ - Config       │
│ - Roles        │ │            │ │                │
└────────┬───────┘ └─────┬──────┘ └────────┬───────┘
         │               │                  │
         ▼               ▼                  ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│ KBAIdentityDb│  │ KBAProductDb │  │ KBATenantDb  │
└──────────────┘  └──────────────┘  └──────────────┘
```

## 🚀 Démarrage Rapide

### 1. Configuration des bases de données

Chaque microservice a sa propre base de données. Modifiez les `appsettings.json` :

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=KBA[Service]Db;Trusted_Connection=true;TrustServerCertificate=true"
  }
}
```

### 2. Appliquer les migrations

```powershell
# Identity Service
cd microservices\KBA.IdentityService
dotnet ef database update

# Product Service
cd ..\KBA.ProductService
dotnet ef database update

# Tenant Service
cd ..\KBA.TenantService
dotnet ef database update
```

### 3. Démarrer les services

**Option A: Manuel (4 terminaux séparés)**

```powershell
# Terminal 1 - Identity Service
cd microservices\KBA.IdentityService
dotnet run

# Terminal 2 - Product Service
cd microservices\KBA.ProductService
dotnet run

# Terminal 3 - Tenant Service
cd microservices\KBA.TenantService
dotnet run

# Terminal 4 - API Gateway
cd microservices\KBA.ApiGateway
dotnet run
```

**Option B: Script automatisé**

```powershell
.\start-microservices.ps1
```

### 4. Accéder à l'API

- **API Gateway**: http://localhost:5000
- **Swagger Gateway**: http://localhost:5000/swagger

## 📡 Endpoints

### Via API Gateway (Port 5000)

#### Authentication
- POST `/api/identity/auth/login` - Se connecter
- POST `/api/identity/auth/register` - S'enregistrer

#### Users
- GET `/api/identity/users` - Liste des utilisateurs
- GET `/api/identity/users/{id}` - Détails utilisateur
- POST `/api/identity/users` - Créer utilisateur

#### Products
- GET `/api/products` - Liste des produits
- GET `/api/products/{id}` - Détails produit
- POST `/api/products` - Créer produit
- PUT `/api/products/{id}` - Mettre à jour produit
- DELETE `/api/products/{id}` - Supprimer produit

#### Tenants
- GET `/api/tenants` - Liste des tenants
- GET `/api/tenants/{id}` - Détails tenant
- POST `/api/tenants` - Créer tenant

## 🔧 Configuration

### JWT (Partagé entre tous les services)

Assurez-vous que tous les services utilisent les mêmes paramètres JWT dans `appsettings.json` :

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

### Service Discovery (Optionnel)

Pour la production, considérez l'utilisation de :
- **Consul** pour la découverte de services
- **Kubernetes** pour l'orchestration
- **Docker Compose** pour le développement local

## 🐳 Docker

Chaque microservice inclut un `Dockerfile`. Pour construire et démarrer :

```powershell
docker-compose up -d
```

## 📊 Monitoring

- **Health Checks**: Chaque service expose `/health`
- **Logs centralisés**: Configuration Serilog avec Seq
- **Métriques**: Prometheus endpoints sur `/metrics`

## 🧪 Tests

```powershell
# Tester tous les microservices
dotnet test

# Tester un service spécifique
cd microservices\KBA.IdentityService.Tests
dotnet test
```

## 🔄 Migration depuis le Monolithe

Si vous migrez depuis l'ancienne architecture monolithique :

1. Les données sont maintenant séparées par service
2. Utilisez les scripts de migration fournis dans `/migration-scripts`
3. Consultez `MIGRATION-GUIDE.md` pour les détails

## 📚 Documentation Détaillée

- [Architecture Details](./docs/ARCHITECTURE.md)
- [Deployment Guide](./docs/DEPLOYMENT.md)
- [API Documentation](./docs/API.md)
- [Development Guide](./docs/DEVELOPMENT.md)

## 🔐 Sécurité

- Authentification JWT via Identity Service
- API Gateway valide tous les tokens
- Communication inter-services via JWT
- HTTPS obligatoire en production
- Rate limiting configuré

## ⚡ Performance

- Caching Redis disponible
- Connection pooling optimisé
- Pagination automatique
- Compression activée
- CDN pour les assets statiques

---

## 📚 Documentation Complète

### Guides Principaux

1. **[REPONSES-VOS-QUESTIONS.md](./docs/REPONSES-VOS-QUESTIONS.md)** ⭐
   - Base de données unique
   - Communication entre services
   - Ajouter un nouveau service
   - Utilisation du dossier src/
   - Débogage
   - Déploiement Docker + IIS
   - Logging complet

2. **[QUICKSTART.md](./docs/QUICKSTART.md)**
   - Démarrage en 3 étapes
   - Tests des endpoints
   - Troubleshooting

3. **[docs/FAQ-COMPLETE.md](./docs/FAQ-COMPLETE.md)**
   - FAQ exhaustive
   - Tous les détails techniques

4. **[docs/ADD-NEW-SERVICE.md](./docs/ADD-NEW-SERVICE.md)**
   - Guide complet avec exemple Order Service
   - Template réutilisable

5. **[docs/DEBUGGING-GUIDE.md](./docs/DEBUGGING-GUIDE.md)**
   - Visual Studio multi-startup
   - VS Code configuration
   - Attach to process
   - Remote debugging Docker

6. **[docs/ARCHITECTURE.md](./docs/ARCHITECTURE.md)**
   - Patterns utilisés
   - Scalabilité
   - Monitoring

### Scripts Utiles

- **`start-microservices.ps1`** - Démarrer tous les services
- **`deploy-microservices-iis.ps1`** - Déployer sur IIS
- **`docker-compose.yml`** - Déployer sur Docker

---

**KBA Framework Microservices** - Architecture scalable et production-ready
