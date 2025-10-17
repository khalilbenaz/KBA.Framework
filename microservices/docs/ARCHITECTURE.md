# Architecture Microservices - KBA Framework

## 📐 Vue d'Ensemble

Le KBA Framework a été transformé d'une architecture monolithique en une architecture microservices moderne suivant les principes de **Domain-Driven Design** et **Clean Architecture**.

## 🏗️ Principes Architecturaux

### 1. Bounded Contexts

Chaque microservice correspond à un **bounded context** DDD :

- **Identity Context** : Gestion des utilisateurs, authentification, autorisation
- **Product Context** : Gestion du catalogue produits
- **Tenant Context** : Multi-tenancy et configuration des organisations

### 2. Indépendance des Services

✅ **Base de données par service** : Chaque microservice a sa propre base de données  
✅ **Déploiement indépendant** : Les services peuvent être déployés séparément  
✅ **Scalabilité indépendante** : Chaque service peut être scalé selon ses besoins  
✅ **Technologies hétérogènes** : Possibilité d'utiliser différentes technologies par service

### 3. Communication

#### Synchrone (HTTP/REST)
- **API Gateway → Services** : Routage des requêtes HTTP
- **Service → Service** : Appels REST directs (limités, à éviter si possible)

#### Asynchrone (Optionnel)
- **RabbitMQ** : Pour les événements inter-services
- **Event Bus** : Pour la cohérence éventuelle

## 🔧 Composants de l'Architecture

### API Gateway (Port 5000)

**Responsabilités** :
- Point d'entrée unique pour tous les clients
- Routage des requêtes vers les microservices appropriés
- Authentification centralisée (validation JWT)
- Rate limiting et throttling
- Agrégation de données (si nécessaire)
- CORS et sécurité

**Technologies** :
- Ocelot (API Gateway)
- ASP.NET Core 8
- Serilog (logging)

**Configuration** :
```json
{
  "Routes": [
    {
      "DownstreamPathTemplate": "/api/{everything}",
      "DownstreamHostAndPorts": [{ "Host": "localhost", "Port": 5001 }],
      "UpstreamPathTemplate": "/api/identity/{everything}"
    }
  ]
}
```

### Identity Service (Port 5001)

**Responsabilités** :
- Authentification des utilisateurs (login/register)
- Génération de tokens JWT
- Gestion du cycle de vie des utilisateurs
- Gestion des rôles et permissions

**Base de données** : `KBAIdentityDb`

**Entities** :
- `User` : Utilisateurs du système
- `Role` : Rôles (Admin, User, etc.)

**Endpoints** :
- `POST /api/auth/login` : Connexion
- `POST /api/auth/register` : Enregistrement
- `GET /api/users` : Liste des utilisateurs
- `GET /api/users/{id}` : Détails utilisateur
- `POST /api/users` : Créer un utilisateur
- `PUT /api/users/{id}` : Modifier un utilisateur
- `DELETE /api/users/{id}` : Supprimer un utilisateur

**Dépendances** :
- KBA.Framework.Domain (entités partagées)
- BCrypt.Net (hachage des mots de passe)
- JWT Bearer Authentication

### Product Service (Port 5002)

**Responsabilités** :
- Gestion du catalogue de produits
- CRUD complet sur les produits
- Gestion du stock
- Catégorisation des produits

**Base de données** : `KBAProductDb`

**Entities** :
- `Product` : Produits du catalogue

**Endpoints** :
- `GET /api/products` : Liste des produits (public)
- `GET /api/products/{id}` : Détails produit (public)
- `POST /api/products` : Créer un produit (authentifié)
- `PUT /api/products/{id}` : Modifier un produit (authentifié)
- `DELETE /api/products/{id}` : Supprimer un produit (authentifié)

**Dépendances** :
- KBA.Framework.Domain (entités partagées)
- JWT Bearer Authentication (validation uniquement)

### Tenant Service (Port 5003)

**Responsabilités** :
- Gestion des tenants (organisations/clients)
- Configuration multi-tenant
- Activation/désactivation des tenants

**Base de données** : `KBATenantDb`

**Entities** :
- `Tenant` : Organisations/clients

**Endpoints** :
- `GET /api/tenants` : Liste des tenants (authentifié)
- `GET /api/tenants/{id}` : Détails tenant (authentifié)
- `POST /api/tenants` : Créer un tenant (authentifié)
- `PUT /api/tenants/{id}` : Modifier un tenant (authentifié)
- `DELETE /api/tenants/{id}` : Supprimer un tenant (authentifié)

**Dépendances** :
- KBA.Framework.Domain (entités partagées)
- JWT Bearer Authentication

## 🔐 Sécurité

### JWT (JSON Web Tokens)

**Configuration partagée** : Tous les services utilisent la même clé secrète JWT pour valider les tokens.

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

**Flow d'authentification** :

```
1. Client → API Gateway → Identity Service : POST /api/identity/auth/login
2. Identity Service → Client : { token: "eyJhbGc..." }
3. Client → API Gateway → Product Service : GET /api/products
   Header: Authorization: Bearer eyJhbGc...
4. Product Service valide le token → Retourne les données
```

### Authorization

Chaque service peut implémenter ses propres règles d'autorisation :
- Basé sur les rôles (Role-Based Access Control)
- Basé sur les claims du JWT
- Basé sur le TenantId pour l'isolation multi-tenant

## 📊 Patterns Utilisés

### 1. API Gateway Pattern
- Point d'entrée unique
- Routage intelligent
- Agrégation de requêtes

### 2. Database per Service
- Chaque service a sa propre base de données
- Isolation complète des données
- Scalabilité indépendante

### 3. Strangler Fig Pattern
- Migration progressive du monolithe vers les microservices
- Coexistence des deux architectures possible

### 4. Service Registry & Discovery (Futur)
- Consul pour la découverte de services
- Configuration dynamique des routes

### 5. Circuit Breaker (Futur)
- Polly pour la résilience
- Gestion des pannes en cascade

## 🔄 Communication Inter-Services

### Synchrone (Actuel)
```
Client → API Gateway → Service
```

### Asynchrone (Recommandé pour production)

**Événements de domaine** :
```csharp
public class UserCreatedEvent
{
    public Guid UserId { get; set; }
    public string Email { get; set; }
    public Guid? TenantId { get; set; }
}
```

**Intégration avec RabbitMQ** :
```
Identity Service → RabbitMQ → [Product Service, Tenant Service]
```

## 📈 Scalabilité

### Scaling Horizontal

Chaque service peut être répliqué indépendamment :

```
API Gateway (x1)
    ├─ Identity Service (x2)
    ├─ Product Service (x5)  ← Service le plus sollicité
    └─ Tenant Service (x1)
```

### Load Balancing

```yaml
# Kubernetes Deployment
apiVersion: apps/v1
kind: Deployment
metadata:
  name: product-service
spec:
  replicas: 5
  selector:
    matchLabels:
      app: product-service
```

## 🗄️ Gestion des Données

### Transactions Distribuées

**Saga Pattern** : Pour les transactions multi-services

```
Scénario : Créer un produit avec validation de tenant
1. Tenant Service : Valider le tenant
2. Product Service : Créer le produit
3. Si échec → Compensation (rollback)
```

### Cohérence Éventuelle

Les données peuvent être temporairement incohérentes entre services, mais convergent vers un état cohérent via des événements asynchrones.

## 🏷️ Versioning des APIs

```
/api/v1/products
/api/v2/products
```

Chaque service peut avoir plusieurs versions d'API actives simultanément.

## 📊 Monitoring & Observabilité

### Logging Centralisé
- **Serilog** : Logs structurés
- **Seq** ou **ELK Stack** : Agrégation des logs
- **Correlation ID** : Traçabilité des requêtes

### Métriques
- **Prometheus** : Collection de métriques
- **Grafana** : Visualisation

### Tracing Distribué
- **OpenTelemetry** : Instrumentation
- **Jaeger** : Visualisation des traces

### Health Checks

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<IdentityDbContext>()
    .AddUrlGroup(new Uri("http://localhost:5002/health"), "product-service");
```

## 🚀 Déploiement

### Environnements

```
Development  → LocalDB, Swagger activé
Staging      → Azure SQL, Monitoring
Production   → Azure SQL, HA, Scaling automatique
```

### Containers (Docker)

Chaque service a son propre `Dockerfile` :

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
COPY . .
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "KBA.IdentityService.dll"]
```

### Orchestration (Kubernetes)

```yaml
apiVersion: v1
kind: Service
metadata:
  name: identity-service
spec:
  selector:
    app: identity-service
  ports:
    - protocol: TCP
      port: 80
      targetPort: 80
```

## 🔮 Évolutions Futures

### Phase 2
- [ ] Service Discovery (Consul)
- [ ] Circuit Breaker (Polly)
- [ ] Event Bus (RabbitMQ)
- [ ] Redis Cache distribué

### Phase 3
- [ ] Kubernetes deployment
- [ ] Service Mesh (Istio)
- [ ] CQRS pattern
- [ ] Event Sourcing

### Phase 4
- [ ] GraphQL API Gateway
- [ ] gRPC pour communication inter-services
- [ ] Serverless functions

## 📚 Références

- [Microservices Pattern](https://microservices.io/patterns/)
- [Domain-Driven Design](https://www.domainlanguage.com/ddd/)
- [Ocelot Documentation](https://ocelot.readthedocs.io/)
- [.NET Microservices Architecture](https://docs.microsoft.com/dotnet/architecture/microservices/)

---

**Architecture créée avec ❤️ pour le KBA Framework**
