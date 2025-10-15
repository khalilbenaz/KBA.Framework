# Installation des Packages NuGet Requis

Ce document liste tous les packages NuGet nécessaires pour les nouvelles fonctionnalités.

## 📦 Packages à Installer

### KBA.Framework.Api

```powershell
# Installer dans le projet API
cd src/KBA.Framework.Api

# FluentValidation pour la validation
dotnet add package FluentValidation.AspNetCore --version 11.3.0

# JWT Authentication
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0

# Serilog pour le logging
dotnet add package Serilog.AspNetCore --version 8.0.0
dotnet add package Serilog.Sinks.Console --version 5.0.0
dotnet add package Serilog.Sinks.File --version 5.0.0
```

### KBA.Framework.Application

```powershell
# Installer dans le projet Application
cd src/KBA.Framework.Application

# FluentValidation
dotnet add package FluentValidation --version 11.9.0
```

### KBA.Framework.Infrastructure

```powershell
# Installer dans le projet Infrastructure (si besoin)
cd src/KBA.Framework.Infrastructure

# Aucun nouveau package requis pour l'instant
```

## 🔍 Vérification de l'Installation

### Commande de Vérification

```powershell
# Depuis la racine du projet
dotnet list src/KBA.Framework.Api/KBA.Framework.Api.csproj package
dotnet list src/KBA.Framework.Application/KBA.Framework.Application.csproj package
```

### Packages Attendus dans KBA.Framework.Api

```
Top-level Package                                    Requested
--------------------------------------------------------
FluentValidation.AspNetCore                         11.3.0
Microsoft.AspNetCore.Authentication.JwtBearer       8.0.0
Serilog.AspNetCore                                  8.0.0
Serilog.Sinks.Console                               5.0.0
Serilog.Sinks.File                                  5.0.0
Microsoft.EntityFrameworkCore.SqlServer             8.0.x
Swashbuckle.AspNetCore                              6.x.x
```

### Packages Attendus dans KBA.Framework.Application

```
Top-level Package                                    Requested
--------------------------------------------------------
FluentValidation                                    11.9.0
```

## 🚀 Compilation et Tests

### 1. Restaurer les Packages

```powershell
# Depuis la racine
dotnet restore
```

### 2. Compiler la Solution

```powershell
dotnet build
```

### 3. Vérifier les Erreurs

Si vous obtenez des erreurs de compilation, vérifiez:

1. **Erreur FluentValidation non trouvé:**
   ```powershell
   dotnet add src/KBA.Framework.Api/KBA.Framework.Api.csproj package FluentValidation.AspNetCore
   dotnet add src/KBA.Framework.Application/KBA.Framework.Application.csproj package FluentValidation
   ```

2. **Erreur JwtBearer non trouvé:**
   ```powershell
   dotnet add src/KBA.Framework.Api/KBA.Framework.Api.csproj package Microsoft.AspNetCore.Authentication.JwtBearer
   ```

3. **Erreur Serilog non trouvé:**
   ```powershell
   dotnet add src/KBA.Framework.Api/KBA.Framework.Api.csproj package Serilog.AspNetCore
   dotnet add src/KBA.Framework.Api/KBA.Framework.Api.csproj package Serilog.Sinks.Console
   dotnet add src/KBA.Framework.Api/KBA.Framework.Api.csproj package Serilog.Sinks.File
   ```

## 📋 Checklist de Vérification

Avant de lancer l'application:

- [ ] Tous les packages NuGet sont installés
- [ ] La solution compile sans erreur
- [ ] Le fichier `appsettings.json` contient les nouvelles sections
- [ ] La `SecretKey` JWT est configurée
- [ ] Le dossier `logs/` sera créé automatiquement au démarrage

## 🔧 Scripts d'Installation Rapide

### PowerShell (Windows)

Créez un fichier `install-packages.ps1`:

```powershell
# Installation des packages pour KBA.Framework

Write-Host "Installation des packages pour KBA.Framework.Api..." -ForegroundColor Green
cd src/KBA.Framework.Api
dotnet add package FluentValidation.AspNetCore --version 11.3.0
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0
dotnet add package Serilog.AspNetCore --version 8.0.0
dotnet add package Serilog.Sinks.Console --version 5.0.0
dotnet add package Serilog.Sinks.File --version 5.0.0
cd ../..

Write-Host "Installation des packages pour KBA.Framework.Application..." -ForegroundColor Green
cd src/KBA.Framework.Application
dotnet add package FluentValidation --version 11.9.0
cd ../..

Write-Host "Restauration des packages..." -ForegroundColor Green
dotnet restore

Write-Host "Compilation de la solution..." -ForegroundColor Green
dotnet build

Write-Host "Installation terminée!" -ForegroundColor Green
```

Exécutez avec:
```powershell
.\install-packages.ps1
```

### Bash (Linux/Mac)

Créez un fichier `install-packages.sh`:

```bash
#!/bin/bash

echo "Installation des packages pour KBA.Framework.Api..."
cd src/KBA.Framework.Api
dotnet add package FluentValidation.AspNetCore --version 11.3.0
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0
dotnet add package Serilog.AspNetCore --version 8.0.0
dotnet add package Serilog.Sinks.Console --version 5.0.0
dotnet add package Serilog.Sinks.File --version 5.0.0
cd ../..

echo "Installation des packages pour KBA.Framework.Application..."
cd src/KBA.Framework.Application
dotnet add package FluentValidation --version 11.9.0
cd ../..

echo "Restauration des packages..."
dotnet restore

echo "Compilation de la solution..."
dotnet build

echo "Installation terminée!"
```

Exécutez avec:
```bash
chmod +x install-packages.sh
./install-packages.sh
```

## 🎯 Prochaines Étapes

Après l'installation des packages:

1. Vérifier que la compilation réussit: `dotnet build`
2. Lancer l'application: `dotnet run --project src/KBA.Framework.Api`
3. Tester l'endpoint Swagger: `http://localhost:5000/swagger`
4. Tester l'authentification: `POST /api/auth/login`
5. Vérifier les logs dans le dossier `logs/`

## 📞 Support

En cas de problème:
1. Vérifier la version de .NET SDK: `dotnet --version` (doit être 8.0 ou supérieur)
2. Nettoyer et recompiler: `dotnet clean && dotnet build`
3. Supprimer les dossiers `bin/` et `obj/` puis relancer `dotnet restore`

---

**Date:** 15 octobre 2025
**Version:** 1.0.0
