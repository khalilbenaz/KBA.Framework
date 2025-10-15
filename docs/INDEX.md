# Documentation KBA Framework

Ce dossier contient toute la documentation technique du projet KBA Framework.

## 📚 Liste des documents

### Guides de démarrage
- **[GUIDE-COMPLET.md](GUIDE-COMPLET.md)** - Guide complet d'utilisation du framework
- **[GUIDE_TEST_RAPIDE.md](GUIDE_TEST_RAPIDE.md)** - Guide de test rapide pour démarrer
- **[INSTALLATION_PACKAGES.md](INSTALLATION_PACKAGES.md)** - Installation et configuration des packages

### Documentation technique
- **[TENANTID_IMPLEMENTATION.md](TENANTID_IMPLEMENTATION.md)** - Implémentation du multi-tenancy et du TenantId
- **[AUTHORIZATION_SUMMARY.md](AUTHORIZATION_SUMMARY.md)** - Sécurité et autorisation JWT
- **[AMELIORATIONS_IMPLEMENTEES.md](AMELIORATIONS_IMPLEMENTEES.md)** - Liste des améliorations et optimisations

## 🚀 Démarrage rapide

1. Lire le [README.md](../README.md) à la racine du projet
2. Consulter [INSTALLATION_PACKAGES.md](INSTALLATION_PACKAGES.md) pour l'installation
3. Suivre [GUIDE_TEST_RAPIDE.md](GUIDE_TEST_RAPIDE.md) pour tester l'API
4. Lire [GUIDE-COMPLET.md](GUIDE-COMPLET.md) pour une compréhension approfondie

## 📋 Structure du projet

```
KBA.Framework/
├── docs/                          # Documentation (ce dossier)
├── src/                           # Code source
│   ├── KBA.Framework.Api/        # API REST
│   ├── KBA.Framework.Application/ # Logique métier
│   ├── KBA.Framework.Domain/     # Entités et règles métier
│   └── KBA.Framework.Infrastructure/ # Accès aux données
├── tests/                         # Tests unitaires et d'intégration
└── README.md                      # Présentation générale
```

## 🔐 Sécurité et Multi-tenancy

Le framework implémente:
- **JWT Authentication** avec claims personnalisés
- **Multi-tenancy** avec isolation par TenantId
- **Autorisation** via `[Authorize]` sur les contrôleurs
- **Contexte utilisateur** injectable via `ICurrentUserContext`

Voir [AUTHORIZATION_SUMMARY.md](AUTHORIZATION_SUMMARY.md) et [TENANTID_IMPLEMENTATION.md](TENANTID_IMPLEMENTATION.md) pour plus de détails.

## 🛠️ Technologies utilisées

- **ASP.NET Core 8.0** - Framework web
- **Entity Framework Core** - ORM
- **FluentValidation** - Validation
- **Serilog** - Logging
- **JWT Bearer** - Authentification
- **Swagger/ReDoc** - Documentation API

## 📝 Contribuer

Pour toute amélioration ou correction, consulter [AMELIORATIONS_IMPLEMENTEES.md](AMELIORATIONS_IMPLEMENTEES.md) pour voir l'historique des modifications.
