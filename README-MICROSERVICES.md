# 🎉 KBA Framework - Architecture Microservices Disponible !

Le KBA Framework supporte maintenant **deux architectures** :

## 🏛️ Architecture Monolithique (Existante)

**Localisation** : `src/`

✅ Simple à déployer  
✅ Idéal pour MVP et petites équipes  
✅ Une seule base de données  
✅ Documentation : [README.md](./README.md)

**Démarrage rapide** :
```powershell
dotnet run --project src/KBA.Framework.Api
# Accès : http://localhost:5220
```

---

## 🚀 Architecture Microservices (Nouvelle)

**Localisation** : `microservices/`

✅ Scalabilité horizontale  
✅ Déploiement indépendant  
✅ Isolation des données  
✅ Documentation : [microservices/README.md](./microservices/README.md)

### Services Disponibles

| Service | Port | Description |
|---------|------|-------------|
| **API Gateway** | 5000 | Point d'entrée unique |
| Identity Service | 5001 | Authentification & Utilisateurs |
| Product Service | 5002 | Gestion des produits |
| Tenant Service | 5003 | Multi-tenancy |

### Démarrage Rapide

```powershell
cd microservices
.\start-microservices.ps1
```

**Accès** : http://localhost:5000

### Architecture

```
Client → API Gateway (5000) → {
    ├─ Identity Service (5001) → KBAIdentityDb
    ├─ Product Service (5002)  → KBAProductDb
    └─ Tenant Service (5003)   → KBATenantDb
}
```

---

## 📚 Documentation

### Guides Principaux
- **[MONOLITH-VS-MICROSERVICES.md](./MONOLITH-VS-MICROSERVICES.md)** - Comparaison détaillée
- **[microservices/QUICKSTART.md](./microservices/QUICKSTART.md)** - Démarrage en 3 étapes
- **[microservices/docs/ARCHITECTURE.md](./microservices/docs/ARCHITECTURE.md)** - Architecture détaillée

### Monolithe
- [README.md](./README.md) - Documentation complète
- [src/KBA.Framework.Api/](./src/KBA.Framework.Api/) - Code source

### Microservices
- [microservices/README.md](./microservices/README.md) - Vue d'ensemble
- [microservices/docker-compose.yml](./microservices/docker-compose.yml) - Docker
- [microservices/start-microservices.ps1](./microservices/start-microservices.ps1) - Script de démarrage

---

## 🎯 Quelle Architecture Choisir ?

### Choisissez le Monolithe si :
- 👥 Équipe < 5 développeurs
- 📊 Trafic < 1000 req/sec
- 🚀 MVP ou prototype rapide
- 💰 Budget infrastructure limité

### Choisissez les Microservices si :
- 👥 Équipe > 10 développeurs
- 📈 Besoin de scalabilité
- 🔄 Déploiements fréquents
- 🌐 Services à cycles de vie différents
- 🛡️ Résilience critique

---

## ⚡ Démarrage Ultra-Rapide

### Monolithe
```powershell
dotnet restore
dotnet ef database update --project src/KBA.Framework.Infrastructure --startup-project src/KBA.Framework.Api
dotnet run --project src/KBA.Framework.Api
```

### Microservices
```powershell
cd microservices
dotnet restore KBA.Microservices.sln
.\start-microservices.ps1
```

---

## 🧪 Tester l'API

### Via Monolithe
```bash
# Login
curl -X POST http://localhost:5220/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"Admin@123456"}'

# Créer un produit
curl -X POST http://localhost:5220/api/products \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"iPhone","sku":"IP-001","price":999,"stock":10}'
```

### Via Microservices
```bash
# Login
curl -X POST http://localhost:5000/api/identity/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"Admin@123456"}'

# Créer un produit
curl -X POST http://localhost:5000/api/products \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"iPhone","sku":"IP-001","price":999,"stock":10}'
```

---

## 🐳 Docker

### Monolithe
```powershell
docker build -t kba-monolith -f src/KBA.Framework.Api/Dockerfile .
docker run -p 5220:80 kba-monolith
```

### Microservices
```powershell
cd microservices
docker-compose up -d
```

---

## 📊 Comparaison Rapide

| Feature | Monolithe | Microservices |
|---------|-----------|---------------|
| Complexité | ⭐ Simple | ⭐⭐⭐ Complexe |
| Scalabilité | ⭐⭐ Verticale | ⭐⭐⭐⭐⭐ Horizontale |
| Déploiement | ⭐ Tout ou rien | ⭐⭐⭐⭐⭐ Indépendant |
| Maintenance | ⭐⭐⭐ Court terme | ⭐⭐⭐⭐⭐ Long terme |
| Coût infra | ⭐ Faible | ⭐⭐⭐ Élevé |

---

## 🔄 Migration

Vous pouvez migrer progressivement du monolithe vers les microservices grâce au **Strangler Fig Pattern**.

Consultez [MONOLITH-VS-MICROSERVICES.md](./MONOLITH-VS-MICROSERVICES.md) pour le guide de migration.

---

## 💡 Prochaines Étapes

1. **Testez le monolithe** pour comprendre le domaine
2. **Lisez la documentation** des microservices
3. **Choisissez l'architecture** selon vos besoins
4. **Déployez en production** avec Docker ou Kubernetes

---

## 🤝 Contribution

Les deux architectures sont maintenues activement. Contributions bienvenues !

---

## 📄 Licence

MIT - Voir [LICENSE](./LICENSE)

---

**KBA Framework** - Du monolithe aux microservices, choisissez votre aventure ! 🚀
