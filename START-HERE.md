# 🚀 KBA Framework - Commencez Ici !

## Quelle Architecture Utiliser ?

Ce projet offre **DEUX architectures** au choix :

---

## 🏛️ Option 1 : Architecture Monolithique

**📂 Localisation** : `src/`

### Quand l'utiliser ?
- ✅ Équipe < 5 développeurs
- ✅ MVP ou prototype
- ✅ Application simple à moyenne
- ✅ Budget infrastructure limité

### Démarrage
```powershell
dotnet run --project src/KBA.Framework.Api
```

**Accès** : http://localhost:5220

### Documentation
- [README.md](./README.md) - Documentation complète du monolithe

---

## 🚀 Option 2 : Architecture Microservices

**📂 Localisation** : `microservices/`

### Quand l'utiliser ?
- ✅ Équipe > 10 développeurs
- ✅ Besoin de scalabilité
- ✅ Déploiements fréquents
- ✅ Services à cycles de vie différents

### Démarrage
```powershell
cd microservices
.\start-microservices.ps1
```

**Accès** : http://localhost:5000

### Documentation
- **⭐ [microservices/REPONSES-VOS-QUESTIONS.md](./microservices/REPONSES-VOS-QUESTIONS.md)** - Toutes vos questions
- [microservices/QUICKSTART.md](./microservices/QUICKSTART.md) - Démarrage rapide
- [microservices/INDEX-DOCUMENTATION.md](./microservices/INDEX-DOCUMENTATION.md) - Index complet
- [microservices/README.md](./microservices/README.md) - Documentation principale

---

## 📊 Comparaison Rapide

| Critère | Monolithe | Microservices |
|---------|-----------|---------------|
| **Complexité** | ⭐ Simple | ⭐⭐⭐ Complexe |
| **Scalabilité** | ⭐⭐ Verticale | ⭐⭐⭐⭐⭐ Horizontale |
| **Déploiement** | ⭐ Tout ou rien | ⭐⭐⭐⭐⭐ Indépendant |
| **Temps de démarrage** | ⚡ Rapide | ⚡⚡ Moyen |
| **Maintenance** | ⭐⭐⭐ Court terme | ⭐⭐⭐⭐⭐ Long terme |
| **Production** | Petit/Moyen | Moyen/Grand |

**Détails** : [MONOLITH-VS-MICROSERVICES.md](./MONOLITH-VS-MICROSERVICES.md)

---

## 🎯 Votre Situation

### Choisissez le Monolithe si :
- 👥 Vous êtes seul ou en petite équipe
- 🚀 Vous voulez un MVP rapidement
- 💰 Budget infrastructure limité
- 📚 Vous découvrez l'architecture Clean

### Choisissez les Microservices si :
- 👥 Équipe de 10+ développeurs
- 📈 Besoin de scalabilité importante
- 🔄 Déploiements très fréquents
- 🌐 Application multi-régions
- 💪 Expérience avec les systèmes distribués

---

## 📚 Documentation par Architecture

### Monolithe
```
src/
├── KBA.Framework.Api/           ← API principale
├── KBA.Framework.Application/   ← Services
├── KBA.Framework.Infrastructure/← Repositories, DbContext
└── KBA.Framework.Domain/        ← Entités
```

**Guide** : [README.md](./README.md)

### Microservices
```
microservices/
├── KBA.ApiGateway/          ← Point d'entrée (Port 5000)
├── KBA.IdentityService/     ← Auth & Users (Port 5001)
├── KBA.ProductService/      ← Produits (Port 5002)
├── KBA.TenantService/       ← Multi-tenancy (Port 5003)
└── docs/                    ← 7 guides complets
```

**Guides** :
- ⭐ **[REPONSES-VOS-QUESTIONS.md](./microservices/REPONSES-VOS-QUESTIONS.md)** - Toutes vos questions répondues
- **[QUICKSTART.md](./microservices/QUICKSTART.md)** - Démarrage en 3 étapes
- **[INDEX-DOCUMENTATION.md](./microservices/INDEX-DOCUMENTATION.md)** - Navigation complète

---

## ⚡ Démarrage Ultra-Rapide

### Monolithe (1 commande)
```powershell
# 1. Restaurer
dotnet restore

# 2. Créer la BD
dotnet ef database update --project src/KBA.Framework.Infrastructure --startup-project src/KBA.Framework.Api

# 3. Démarrer
dotnet run --project src/KBA.Framework.Api

# Accès: http://localhost:5220
```

### Microservices (1 script)
```powershell
cd microservices
.\start-microservices.ps1

# Accès: http://localhost:5000
```

---

## 🔑 Fonctionnalités Communes

Les deux architectures offrent :

✅ **Clean Architecture** - Séparation des responsabilités  
✅ **Domain-Driven Design** - Modélisation riche  
✅ **Multi-Tenancy** - Support SaaS natif  
✅ **JWT Authentication** - Sécurité moderne  
✅ **Entity Framework Core 8** - ORM performant  
✅ **Swagger/OpenAPI** - Documentation interactive  
✅ **Serilog** - Logging structuré  
✅ **Tests** - Unit + Integration tests

---

## 📖 Guides Complets

### Documentation Générale
- [README.md](./README.md) - Monolithe
- [README-MICROSERVICES.md](./README-MICROSERVICES.md) - Vue d'ensemble
- [MONOLITH-VS-MICROSERVICES.md](./MONOLITH-VS-MICROSERVICES.md) - Comparaison

### Documentation Microservices (⭐ Pour vos questions)
1. **[REPONSES-VOS-QUESTIONS.md](./microservices/REPONSES-VOS-QUESTIONS.md)** ⭐⭐⭐
   - Base de données unique ✅
   - Communication entre services ✅
   - Ajouter un nouveau service ✅
   - Utilisation du dossier src/ ✅
   - Débogage ✅
   - Déploiement Docker + IIS ✅
   - Logging ✅

2. **[QUICKSTART.md](./microservices/QUICKSTART.md)**
   - Démarrage en 3 étapes
   - Tests rapides

3. **[INDEX-DOCUMENTATION.md](./microservices/INDEX-DOCUMENTATION.md)**
   - Navigation dans toute la doc
   - Index par question

4. **[docs/ADD-NEW-SERVICE.md](./microservices/docs/ADD-NEW-SERVICE.md)**
   - Template complet
   - Exemple Order Service

5. **[docs/DEBUGGING-GUIDE.md](./microservices/docs/DEBUGGING-GUIDE.md)**
   - Visual Studio multi-startup
   - VS Code
   - Docker debugging

6. **[docs/ARCHITECTURE.md](./microservices/docs/ARCHITECTURE.md)**
   - Patterns avancés
   - Scalabilité

7. **[docs/FAQ-COMPLETE.md](./microservices/docs/FAQ-COMPLETE.md)**
   - FAQ exhaustive
   - Exemples de code

---

## 🆘 Problèmes Courants

### Port déjà utilisé
```powershell
netstat -ano | findstr :5001
taskkill /PID <PID> /F
```

### Base de données ne se crée pas
```powershell
# LocalDB
sqllocaldb info
sqllocaldb start MSSQLLocalDB

# Recréer
dotnet ef database drop
dotnet ef database update
```

### Services ne démarrent pas
```powershell
# Nettoyer
dotnet clean
dotnet build

# Vérifier les logs
Get-Content "microservices\*\logs\*.log" -Tail 50
```

---

## 🎓 Parcours Recommandé

### Jour 1 : Découverte
1. ✅ Lire ce fichier (START-HERE.md)
2. ✅ Choisir votre architecture
3. ✅ Suivre le QUICKSTART
4. ✅ Tester l'API avec Swagger

### Jour 2 : Compréhension
1. ✅ Lire README complet
2. ✅ Explorer le code source
3. ✅ Comprendre Clean Architecture
4. ✅ Tester l'authentification JWT

### Jour 3 : Développement
1. ✅ Créer votre premier service (microservices)
2. ✅ Ajouter une entité
3. ✅ Configurer le débogage
4. ✅ Déployer en local

### Semaine 2 : Production
1. ✅ Tests d'intégration
2. ✅ Déploiement Docker ou IIS
3. ✅ Logging centralisé (Seq)
4. ✅ Monitoring

---

## 💡 Conseils

### Pour Débuter
- 🎯 Commencez avec le **monolithe** pour comprendre le domaine
- 📚 Lisez la documentation Clean Architecture
- 🧪 Testez tous les endpoints avec Swagger

### Pour Scaler
- 🚀 Passez aux **microservices** quand nécessaire
- 🐳 Utilisez Docker pour le déploiement
- 📊 Implémentez le monitoring dès le début

### Pour Produire
- ✅ Tous les tests passent
- 🔐 Secrets dans variables d'environnement
- 📝 Logs centralisés
- 💪 HTTPS activé
- 🔄 CI/CD configuré

---

## 🚀 Next Steps

**Vous avez lu ce guide ?**

### Si vous choisissez le Monolithe
→ Allez sur [README.md](./README.md)

### Si vous choisissez les Microservices
→ Allez sur **[microservices/REPONSES-VOS-QUESTIONS.md](./microservices/REPONSES-VOS-QUESTIONS.md)** ⭐

**Bonne chance ! 🎉**

---

## 📞 Structure des Fichiers

```
KBA.Framework/
├── START-HERE.md                        ← VOUS ÊTES ICI
├── README.md                            ← Doc Monolithe
├── README-MICROSERVICES.md              ← Vue d'ensemble
├── MONOLITH-VS-MICROSERVICES.md         ← Comparaison
│
├── src/                                 ← MONOLITHE
│   ├── KBA.Framework.Api/
│   ├── KBA.Framework.Application/
│   ├── KBA.Framework.Infrastructure/
│   └── KBA.Framework.Domain/            ← Partagé avec microservices
│
└── microservices/                       ← MICROSERVICES
    ├── REPONSES-VOS-QUESTIONS.md        ← ⭐ VOS QUESTIONS
    ├── QUICKSTART.md                    ← Démarrage rapide
    ├── INDEX-DOCUMENTATION.md           ← Index complet
    ├── README.md                        ← Doc principale
    ├── RESUME-COMPLET.md                ← Résumé
    │
    ├── KBA.ApiGateway/
    ├── KBA.IdentityService/
    ├── KBA.ProductService/
    ├── KBA.TenantService/
    │
    ├── docs/
    │   ├── ADD-NEW-SERVICE.md
    │   ├── DEBUGGING-GUIDE.md
    │   ├── ARCHITECTURE.md
    │   └── FAQ-COMPLETE.md
    │
    ├── start-microservices.ps1
    ├── deploy-microservices-iis.ps1
    └── docker-compose.yml
```

---

**KBA Framework** - Du prototype à l'entreprise, une architecture qui grandit avec vous ! 🚀
