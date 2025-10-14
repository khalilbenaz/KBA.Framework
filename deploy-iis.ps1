# ====================================================================
# Script de déploiement IIS pour KBA.Framework
# ====================================================================
# Ce script automatise le déploiement de l'application sur IIS
# Prérequis: IIS avec ASP.NET Core Hosting Bundle installé
# ====================================================================

param(
    [string]$SiteName = "KBAFramework",
    [string]$AppPoolName = "KBAFrameworkPool",
    [int]$Port = 8080,
    [string]$PhysicalPath = "C:\inetpub\wwwroot\KBAFramework",
    [string]$HostName = "localhost"
)

# Vérifier les privilèges administrateur
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "ERREUR: Ce script doit être exécuté en tant qu'administrateur!" -ForegroundColor Red
    exit 1
}

Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host "    Déploiement KBA Framework sur IIS" -ForegroundColor Cyan
Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host ""

# Étape 1: Publication de l'application
Write-Host "[1/7] Publication de l'application en mode Release..." -ForegroundColor Yellow
$publishPath = ".\src\KBA.Framework.Api\bin\Release\net8.0\publish"
dotnet publish .\src\KBA.Framework.Api\KBA.Framework.Api.csproj -c Release -o $publishPath

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERREUR: La publication a échoué!" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Publication réussie" -ForegroundColor Green
Write-Host ""

# Étape 2: Importation du module IIS
Write-Host "[2/7] Importation du module IIS..." -ForegroundColor Yellow
Import-Module WebAdministration

if (-not (Get-Module WebAdministration)) {
    Write-Host "ERREUR: Le module IIS n'est pas disponible!" -ForegroundColor Red
    Write-Host "Installez IIS avec: Install-WindowsFeature -name Web-Server -IncludeManagementTools" -ForegroundColor Yellow
    exit 1
}
Write-Host "✓ Module IIS chargé" -ForegroundColor Green
Write-Host ""

# Étape 3: Création du répertoire physique
Write-Host "[3/7] Création du répertoire physique..." -ForegroundColor Yellow
if (-not (Test-Path $PhysicalPath)) {
    New-Item -ItemType Directory -Path $PhysicalPath -Force | Out-Null
    Write-Host "✓ Répertoire créé: $PhysicalPath" -ForegroundColor Green
} else {
    Write-Host "✓ Répertoire existant: $PhysicalPath" -ForegroundColor Green
}
Write-Host ""

# Étape 4: Copie des fichiers
Write-Host "[4/7] Copie des fichiers publiés..." -ForegroundColor Yellow
Copy-Item -Path "$publishPath\*" -Destination $PhysicalPath -Recurse -Force
Write-Host "✓ Fichiers copiés" -ForegroundColor Green
Write-Host ""

# Étape 5: Configuration du pool d'application
Write-Host "[5/7] Configuration du pool d'application..." -ForegroundColor Yellow

# Arrêter le pool s'il existe
if (Test-Path "IIS:\AppPools\$AppPoolName") {
    Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    Remove-WebAppPool -Name $AppPoolName
    Write-Host "  - Pool existant supprimé" -ForegroundColor Gray
}

# Créer le nouveau pool
New-WebAppPool -Name $AppPoolName | Out-Null
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name "enable32BitAppOnWin64" -Value $false
Write-Host "✓ Pool d'application créé: $AppPoolName" -ForegroundColor Green
Write-Host ""

# Étape 6: Configuration du site web
Write-Host "[6/7] Configuration du site web..." -ForegroundColor Yellow

# Supprimer le site s'il existe
if (Test-Path "IIS:\Sites\$SiteName") {
    Remove-WebSite -Name $SiteName
    Write-Host "  - Site existant supprimé" -ForegroundColor Gray
}

# Créer le nouveau site
New-WebSite -Name $SiteName `
            -PhysicalPath $PhysicalPath `
            -ApplicationPool $AppPoolName `
            -Port $Port `
            -HostHeader $HostName | Out-Null

Write-Host "✓ Site web créé: $SiteName" -ForegroundColor Green
Write-Host ""

# Étape 7: Configuration des permissions
Write-Host "[7/7] Configuration des permissions..." -ForegroundColor Yellow

# Créer le dossier logs
$logsPath = Join-Path $PhysicalPath "logs"
if (-not (Test-Path $logsPath)) {
    New-Item -ItemType Directory -Path $logsPath -Force | Out-Null
}

# Donner les permissions au pool
$acl = Get-Acl $PhysicalPath
$identity = "IIS AppPool\$AppPoolName"
$fileSystemRights = [System.Security.AccessControl.FileSystemRights]::Modify
$inheritanceFlags = [System.Security.AccessControl.InheritanceFlags]"ContainerInherit, ObjectInherit"
$propagationFlags = [System.Security.AccessControl.PropagationFlags]::None
$accessControlType = [System.Security.AccessControl.AccessControlType]::Allow

$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    $identity, $fileSystemRights, $inheritanceFlags, $propagationFlags, $accessControlType
)

$acl.SetAccessRule($accessRule)
Set-Acl -Path $PhysicalPath -AclObject $acl

Write-Host "✓ Permissions configurées" -ForegroundColor Green
Write-Host ""

# Démarrage du site
Write-Host "Démarrage du site..." -ForegroundColor Yellow
Start-WebSite -Name $SiteName
Start-WebAppPool -Name $AppPoolName
Start-Sleep -Seconds 2

# Résumé
Write-Host ""
Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host "    DÉPLOIEMENT TERMINÉ AVEC SUCCÈS!" -ForegroundColor Green
Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Informations du déploiement:" -ForegroundColor White
Write-Host "  • Nom du site        : $SiteName" -ForegroundColor White
Write-Host "  • Pool d'application : $AppPoolName" -ForegroundColor White
Write-Host "  • Chemin physique    : $PhysicalPath" -ForegroundColor White
Write-Host "  • Port               : $Port" -ForegroundColor White
Write-Host "  • Host               : $HostName" -ForegroundColor White
Write-Host ""
Write-Host "URLs d'accès:" -ForegroundColor White
Write-Host "  • HTTP  : http://${HostName}:${Port}" -ForegroundColor Cyan
Write-Host "  • Swagger: http://${HostName}:${Port}/swagger" -ForegroundColor Cyan
Write-Host ""
Write-Host "Prochaines étapes:" -ForegroundColor Yellow
Write-Host "  1. Configurer la chaîne de connexion dans appsettings.json" -ForegroundColor White
Write-Host "  2. Exécuter les migrations: dotnet ef database update" -ForegroundColor White
Write-Host "  3. Vérifier les logs dans: $logsPath" -ForegroundColor White
Write-Host ""
Write-Host "=====================================================================" -ForegroundColor Cyan
