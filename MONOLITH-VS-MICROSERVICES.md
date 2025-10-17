# KBA Framework : Monolithe vs Microservices

## 📊 Comparaison

| Aspect | Architecture Monolithique | Architecture Microservices |
|--------|--------------------------|----------------------------|
| **Structure** | Application unique | 4 services indépendants |
| **Base de données** | 1 base (KBAFrameworkDb) | 3 bases séparées |
| **Déploiement** | Tout ou rien | Service par service |
| **Scalabilité** | Verticale uniquement | Horizontale par service |
| **Points d'entrée** | 1 API (Port 5220) | API Gateway + 3 services |
| **Complexité** | Simple | Plus complexe |
| **Maintenance** | Plus facile au début | Plus facile à long terme |
| **Tests** | Plus simples | Nécessitent des tests d'intégration |

## 🏗️ Architecture Monolithique (Ancienne)

```
┌─────────────────────────────────────┐
│     KBA.Framework.Api (Port 5220)   │
│  ┌───────────────────────────────┐  │
│  │     Controllers               │  │
│  │  - AuthController             │  │
│  │  - UsersController            │  │
│  │  - ProductsController         │  │
│  └───────────┬───────────────────┘  │
│              │                       │
│  ┌───────────▼───────────────────┐  │
│  │   Application Services        │  │
│  │  - AuthService                │  │
│  │  - UserService                │  │
│  │  - ProductService             │  │
│  └───────────┬───────────────────┘  │
│              │                       │
│  ┌───────────▼───────────────────┐  │
│  │   Infrastructure              │  │
│  │  - Repositories               │  │
│  │  - KBADbContext               │  │
│  └───────────┬───────────────────┘  │
│              │                       │
│  ┌───────────▼───────────────────┐  │
│  │   Domain                      │  │
│  │  - Entities                   │  │
│  └───────────────────────────────┘  │
└──────────────┬──────────────────────┘
               │
               ▼
    ┌──────────────────────┐
    │   KBAFrameworkDb     │
    │  - KBA.Users         │
    │  - KBA.Products      │
    │  - KBA.Tenants       │
    │  - KBA.Roles         │
    │  - ... (15+ tables)  │
    └──────────────────────┘
```

**Localisation** : `src/KBA.Framework.Api/`

## 🏗️ Architecture Microservices (Nouvelle)

```
                    ┌────────────────┐
                    │    Clients     │
                    └────────┬───────┘
                             │
                    ┌────────▼────────┐
                    │  API Gateway    │
                    │  (Port 5000)    │
                    └─┬──────┬───────┬┘
                      │      │       │
        ┌─────────────┘      │       └─────────────┐
        │                    │                     │
┌───────▼────────┐  ┌────────▼────────┐  ┌────────▼────────┐
│ Identity Svc   │  │  Product Svc    │  │  Tenant Svc     │
│  (Port 5001)   │  │  (Port 5002)    │  │  (Port 5003)    │
├────────────────┤  ├─────────────────┤  ├─────────────────┤
│ - Auth         │  │ - Products CRUD │  │ - Tenants CRUD  │
│ - Users        │  │ - Stock Mgmt    │  │ - Configuration │
│ - Roles        │  │ - Categories    │  │                 │
└────────┬───────┘  └─────────┬───────┘  └─────────┬───────┘
         │                    │                     │
         ▼                    ▼                     ▼
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│ KBAIdentityDb   │  │ KBAProductDb    │  │ KBATenantDb     │
│ - KBA.Users     │  │ - KBA.Products  │  │ - KBA.Tenants   │
│ - KBA.Roles     │  │                 │  │                 │
└─────────────────┘  └─────────────────┘  └─────────────────┘
```

**Localisation** : `microservices/`

## 🔄 Transition

### Ce qui reste identique

✅ **Domain Layer** : Les entités du domaine sont partagées  
✅ **Clean Architecture** : Respectée dans chaque microservice  
✅ **JWT Authentication** : Même mécanisme, clé partagée  
✅ **Entity Framework Core** : Même ORM

### Ce qui change

🔄 **Point d'entrée** : 
- Avant : `http://localhost:5220`
- Après : `http://localhost:5000` (API Gateway)

🔄 **Base de données** :
- Avant : 1 base de données
- Après : 3 bases de données séparées

🔄 **Déploiement** :
- Avant : `dotnet run --project src/KBA.Framework.Api`
- Après : `.\microservices\start-microservices.ps1`

🔄 **Endpoints** :
```
Avant : http://localhost:5220/api/auth/login
Après : http://localhost:5000/api/identity/auth/login

Avant : http://localhost:5220/api/products
Après : http://localhost:5000/api/products
```

## 📦 Structure des Fichiers

### Monolithe
```
KBA.Framework/
├── src/
│   ├── KBA.Framework.Domain/
│   ├── KBA.Framework.Application/
│   ├── KBA.Framework.Infrastructure/
│   └── KBA.Framework.Api/           ← 1 seul projet API
└── tests/
```

### Microservices
```
KBA.Framework/
├── src/
│   └── KBA.Framework.Domain/        ← Partagé entre services
├── microservices/
│   ├── KBA.IdentityService/         ← Service 1
│   ├── KBA.ProductService/          ← Service 2
│   ├── KBA.TenantService/           ← Service 3
│   ├── KBA.ApiGateway/              ← Gateway
│   ├── docker-compose.yml
│   └── start-microservices.ps1
└── tests/
```

## 🚀 Quand utiliser chaque architecture ?

### Utilisez le Monolithe si :
- ✅ Équipe < 5 développeurs
- ✅ Trafic < 1000 requêtes/seconde
- ✅ Besoin de simplicité
- ✅ MVP ou prototype
- ✅ Budget limité pour l'infrastructure

### Utilisez les Microservices si :
- ✅ Équipe > 10 développeurs
- ✅ Besoin de scalabilité indépendante
- ✅ Déploiements fréquents
- ✅ Équipes distribuées
- ✅ Services avec des cycles de vie différents
- ✅ Résilience critique

## 📈 Migration Progressive

### Stratégie "Strangler Fig"

Vous pouvez faire coexister les deux architectures :

```
Phase 1 : Monolithe complet
Phase 2 : Monolithe + Identity Microservice
Phase 3 : Monolithe + Identity + Product Microservices
Phase 4 : Tous en microservices
```

### Script de migration de données

```sql
-- Migrer les utilisateurs vers Identity DB
INSERT INTO KBAIdentityDb.dbo.KBA_Users
SELECT * FROM KBAFrameworkDb.dbo.KBA_Users;

-- Migrer les produits vers Product DB
INSERT INTO KBAProductDb.dbo.KBA_Products
SELECT * FROM KBAFrameworkDb.dbo.KBA_Products;

-- Migrer les tenants vers Tenant DB
INSERT INTO KBATenantDb.dbo.KBA_Tenants
SELECT * FROM KBAFrameworkDb.dbo.KBA_Tenants;
```

## 💡 Recommandations

### Pour débuter
1. **Commencez avec le monolithe** (`src/KBA.Framework.Api`)
2. Familiarisez-vous avec le domaine
3. Identifiez les bounded contexts naturels

### Pour scaler
1. **Passez aux microservices** (`microservices/`)
2. Déployez avec Docker Compose ou Kubernetes
3. Ajoutez monitoring et observabilité

### Pour la production

**Option 1 : Monolithe optimisé**
```
- IIS ou Azure App Service
- 1 serveur ou load balancer
- Redis cache
- SQL Server avec réplication
```

**Option 2 : Microservices**
```
- Kubernetes (AKS, EKS, GKE)
- Service mesh (Istio)
- Message broker (RabbitMQ)
- Monitoring (Prometheus + Grafana)
```

## 📚 Ressources

### Documentation Monolithe
- [README.md](./README.md)
- [src/KBA.Framework.Api/](./src/KBA.Framework.Api/)

### Documentation Microservices
- [microservices/README.md](./microservices/README.md)
- [microservices/QUICKSTART.md](./microservices/QUICKSTART.md)
- [microservices/docs/ARCHITECTURE.md](./microservices/docs/ARCHITECTURE.md)

## 🎯 Conclusion

Les deux architectures sont valides et **coexistent dans ce projet** :

- **Monolithe** : Pour le développement rapide et les petites équipes
- **Microservices** : Pour la scalabilité et les grandes équipes

Choisissez selon vos besoins actuels et évoluez progressivement ! 🚀
