# 🗺️ Roadmap des Microservices - Fonctionnalités Manquantes

## 📋 État des Lieux

### Services Actuels (✅ Implémentés)

| Service | Port | Status | Features |
|---------|------|--------|----------|
| **API Gateway** | 5000 | ✅ | Routing, Auth |
| **Identity Service** | 5001 | ✅ | Users, Roles, Auth JWT |
| **Product Service** | 5002 | ✅ | Products, Search, Stock |
| **Tenant Service** | 5003 | ✅ | Tenants, Multi-tenancy |

### Services Manquants (❌ À Créer)

| Service | Port | Priority | Features du Monolithe |
|---------|------|----------|----------------------|
| **Permission Service** | 5004 | 🔴 Haute | Permissions, PermissionGrants |
| **Organization Service** | 5005 | 🟡 Moyenne | OrganizationUnits, UserOrganizationUnits |
| **Configuration Service** | 5006 | 🟡 Moyenne | Settings, FeatureValues |
| **Job Service** | 5007 | 🟢 Basse | BackgroundJobs |
| **Audit Service** | 5008 | 🔴 Haute | AuditLogs, EntityChanges |

---

## 🎯 Stratégie de Migration

### Phase 1 : Services Critiques (Cette Semaine) 🔴

#### 1. Permission Service (Port 5004)

**Problème** : Les permissions sont **essentielles** pour la sécurité

**Solution** : Créer un microservice dédié

**Tables migrées** :
- `KBA.Permissions`
- `KBA.PermissionGrants`
- `KBA.RoleClaims` (déjà dans Identity, mais lié)

**Endpoints** :
```
GET    /api/permissions                    - Liste des permissions
GET    /api/permissions/user/{userId}      - Permissions d'un utilisateur
POST   /api/permissions/check              - Vérifier une permission
POST   /api/permissions/grant              - Accorder une permission
DELETE /api/permissions/revoke             - Révoquer une permission
```

**Communication** :
- Identity Service → Permission Service (vérifier les permissions)
- Tous les services → Permission Service (autorisation)

#### 2. Audit Service (Port 5008)

**Problème** : L'audit est **obligatoire** pour la conformité (RGPD, etc.)

**Solution** : Microservice dédié avec événements

**Tables migrées** :
- `KBA.AuditLogs`
- `KBA.AuditLogActions`
- `KBA.EntityChanges`
- `KBA.EntityPropertyChanges`

**Endpoints** :
```
GET    /api/audit/logs                     - Logs d'audit
GET    /api/audit/logs/user/{userId}       - Logs par utilisateur
GET    /api/audit/logs/entity/{entityId}   - Logs par entité
GET    /api/audit/changes/{entityId}       - Historique des changements
POST   /api/audit/log                      - Enregistrer un log
```

**Communication** :
- **Asynchrone** : Tous les services publient des événements
- Audit Service consomme et stocke

---

### Phase 2 : Services Métier (Semaine Prochaine) 🟡

#### 3. Organization Service (Port 5005)

**Besoin** : Hiérarchie organisationnelle

**Tables migrées** :
- `KBA.OrganizationUnits`
- `KBA.UserOrganizationUnits`

**Endpoints** :
```
GET    /api/organizations                  - Arbre hiérarchique
GET    /api/organizations/{id}             - Détails
GET    /api/organizations/{id}/users       - Utilisateurs de l'unité
POST   /api/organizations                  - Créer unité
PUT    /api/organizations/{id}/move        - Déplacer dans hiérarchie
POST   /api/organizations/{id}/users       - Ajouter utilisateur
```

**Use Case** :
```
Entreprise
├── Direction
│   ├── Marketing (5 users)
│   └── IT (12 users)
└── Production
    ├── Atelier A (20 users)
    └── Atelier B (15 users)
```

#### 4. Configuration Service (Port 5006)

**Besoin** : Configuration centralisée

**Tables migrées** :
- `KBA.Settings`
- `KBA.FeatureValues`

**Endpoints** :
```
GET    /api/config/settings                - Tous les paramètres
GET    /api/config/settings/{key}          - Paramètre spécifique
PUT    /api/config/settings/{key}          - Mettre à jour
GET    /api/config/features                - Features actives
POST   /api/config/features/{name}/toggle  - Activer/Désactiver
```

**Use Case** :
```csharp
// Vérifier si une feature est active
var isEnabled = await _configService.IsFeatureEnabledAsync("AdvancedSearch");

// Obtenir un paramètre
var maxUploadSize = await _configService.GetSettingAsync<int>("MaxUploadSizeMB");
```

---

### Phase 3 : Services Techniques (Plus Tard) 🟢

#### 5. Job Service (Port 5007)

**Besoin** : Tâches en arrière-plan

**Alternative moderne** : **Hangfire** ou **Quartz.NET**

**Tables migrées** :
- `KBA.BackgroundJobs`

**Endpoints** :
```
GET    /api/jobs                           - Liste des jobs
GET    /api/jobs/{id}                      - Détails job
POST   /api/jobs                           - Créer un job
POST   /api/jobs/{id}/retry                - Relancer un job
DELETE /api/jobs/{id}                      - Supprimer un job
```

**Exemples de jobs** :
- Envoi d'emails en masse
- Génération de rapports
- Nettoyage de données
- Import/Export de données

---

## 🏗️ Architecture Complète Recommandée

```
                         API Gateway (Port 5000)
                                 |
        ┌────────────────────────┼────────────────────────┐
        |                        |                        |
   ┌────▼────┐            ┌──────▼──────┐         ┌──────▼──────┐
   | Identity|            |   Product   |         |   Tenant    |
   | (5001)  |            |   (5002)    |         |   (5003)    |
   └────┬────┘            └──────┬──────┘         └──────┬──────┘
        |                        |                        |
        |                        |                        |
   ┌────▼─────────────────┬──────▼──────┬─────────────────▼──────┐
   |                      |             |                        |
┌──▼───────┐       ┌──────▼──────┐  ┌──▼──────────┐   ┌────────▼────┐
|Permission|       |Organization |  |Configuration|   |    Audit    |
| (5004)   |       |   (5005)    |  |   (5006)    |   |   (5008)    |
└──────────┘       └─────────────┘  └─────────────┘   └─────────────┘
                                                              ▲
                                                              |
                                                    ┌─────────┴─────────┐
                                                    |  Event Bus        |
                                                    | (RabbitMQ/Redis)  |
                                                    └───────────────────┘
```

---

## 📊 Comparaison : Intégration vs Microservice

### Option A : Intégrer dans Identity Service

**Permissions** → Intégrer dans Identity Service

**Avantages** :
- ✅ Moins de services à gérer
- ✅ Latence réduite (pas d'appel réseau)
- ✅ Transactions ACID faciles

**Inconvénients** :
- ❌ Identity Service devient gros
- ❌ Couplage fort
- ❌ Scaling non indépendant

### Option B : Service Dédié (Recommandé pour Production)

**Permissions** → Permission Service séparé

**Avantages** :
- ✅ Séparation des responsabilités
- ✅ Scaling indépendant
- ✅ Équipes autonomes
- ✅ Déploiement indépendant

**Inconvénients** :
- ❌ Plus de services (complexité)
- ❌ Latence réseau
- ❌ Transactions distribuées

---

## 🎯 Recommandations par Feature

| Feature | Recommandation | Raison |
|---------|---------------|--------|
| **Permissions** | 🔴 Service dédié | Critique pour sécurité, utilisé partout |
| **Audit** | 🔴 Service dédié | Conformité légale, gros volume |
| **Organization** | 🟡 Intégrer dans Identity OU service | Selon taille entreprise |
| **Configuration** | 🟡 Service dédié | Utile pour feature flags |
| **BackgroundJobs** | 🟢 Hangfire externe | Solution éprouvée existe |

---

## 📝 Plan d'Action Immédiat

### Aujourd'hui : Créer Permission Service

1. **Créer les entités** dans Domain :
```csharp
// src/KBA.Framework.Domain/Entities/Permissions/
- Permission.cs
- PermissionGrant.cs
```

2. **Créer le service** :
```
microservices/KBA.PermissionService/
├── Controllers/PermissionsController.cs
├── Services/PermissionServiceLogic.cs
├── Data/PermissionDbContext.cs
├── DTOs/PermissionDTOs.cs
├── Program.cs
└── appsettings.json
```

3. **Configurer le Gateway** :
```json
{
  "DownstreamPathTemplate": "/api/{everything}",
  "UpstreamPathTemplate": "/api/permissions/{everything}",
  "DownstreamHostAndPorts": [{"Host": "localhost", "Port": 5004}]
}
```

4. **Intégration** : Tous les services vérifient les permissions via ce service

### Cette Semaine : Audit Service

Même démarche pour les logs d'audit.

---

## 🔄 Communication Inter-Services

### Permissions

**Pattern** : Request/Response synchrone avec cache

```csharp
// Dans ProductService
public class ProductsController : ControllerBase
{
    private readonly IPermissionService _permissionService;
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        // Vérifier permission
        var hasPermission = await _permissionService.CheckPermissionAsync(
            User.GetUserId(),
            "Products.Create"
        );
        
        if (!hasPermission)
            return Forbid();
        
        // Créer le produit...
    }
}
```

**Optimisation** : Cache Redis pour les permissions (TTL 5 minutes)

### Audit

**Pattern** : Fire-and-Forget avec événements

```csharp
// Dans ProductService - après création
await _eventBus.PublishAsync(new ProductCreatedEvent
{
    ProductId = product.Id,
    UserId = User.GetUserId(),
    TenantId = product.TenantId,
    Action = "Create",
    Timestamp = DateTime.UtcNow
});

// Audit Service écoute et enregistre automatiquement
```

---

## 💡 Décision Finale

### Pour Votre Projet

**Recommandation** :

1. **Permissions** → ✅ **Créer Permission Service** (Port 5004)
   - Utilisé partout
   - Critique pour sécurité
   - Cache pour performance

2. **Audit** → ✅ **Créer Audit Service** (Port 5008)
   - Conformité obligatoire
   - Événements asynchrones
   - Pas d'impact performance

3. **Organization** → ⚠️ **Intégrer dans Identity Service pour l'instant**
   - Créer plus tard si besoin
   - Évite sur-ingénierie

4. **Configuration** → ⚠️ **Utiliser appsettings.json + Azure App Config**
   - Pas besoin de service pour l'instant
   - Solutions existantes suffisantes

5. **BackgroundJobs** → ⚠️ **Utiliser Hangfire**
   - Ne pas réinventer la roue
   - Solution éprouvée

---

## 📈 Roadmap Timeline

```
Semaine 1 (Actuelle) ✅
- Identity Service
- Product Service (avec recherche avancée)
- Tenant Service
- API Gateway

Semaine 2 🔴
- Permission Service
- Audit Service
- Intégration permissions dans tous les services

Semaine 3 🟡
- Configuration Service (optionnel)
- Organization dans Identity (optionnel)

Semaine 4+ 🟢
- Autres services selon besoins métier
- Order Service (exemple dans doc)
- Notification Service
- Payment Service
- etc.
```

---

## 🎯 Voulez-vous que je crée le Permission Service maintenant ?

Je peux créer :
1. ✅ Entités Permission dans Domain
2. ✅ PermissionService complet (Controllers, Services, DTOs)
3. ✅ Intégration avec Identity Service
4. ✅ Middleware d'autorisation dans tous les services
5. ✅ Tests automatisés
6. ✅ Documentation

**Dites-moi si je continue ! 🚀**
