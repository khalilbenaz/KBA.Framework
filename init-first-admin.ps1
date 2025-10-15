#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Script d'initialisation du KBA Framework - Création du premier administrateur

.DESCRIPTION
    Ce script automatise la création du premier utilisateur administrateur du système KBA Framework.
    Il vérifie d'abord si le système nécessite une initialisation, puis crée l'utilisateur et teste la connexion.

.PARAMETER BaseUrl
    URL de base de l'API. Par défaut: http://localhost:5220

.PARAMETER UserName
    Nom d'utilisateur de l'administrateur. Par défaut: admin

.PARAMETER Email
    Email de l'administrateur. Par défaut: admin@kba-framework.com

.PARAMETER Password
    Mot de passe de l'administrateur. Par défaut: demandé interactivement

.PARAMETER FirstName
    Prénom de l'administrateur. Par défaut: Admin

.PARAMETER LastName
    Nom de famille de l'administrateur. Par défaut: System

.PARAMETER SkipTests
    Si défini, les tests de connexion sont ignorés après la création

.EXAMPLE
    .\init-first-admin.ps1
    Initialise le système avec les valeurs par défaut

.EXAMPLE
    .\init-first-admin.ps1 -BaseUrl "http://localhost:8080"
    Initialise le système sur un port personnalisé

.EXAMPLE
    .\init-first-admin.ps1 -UserName "superadmin" -Email "admin@example.com"
    Initialise le système avec un nom d'utilisateur et email personnalisés

.NOTES
    Auteur: KBA Framework
    Version: 1.0.0
    Date: 2025-10-15
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$BaseUrl = "http://localhost:5220",

    [Parameter(Mandatory = $false)]
    [string]$UserName = "admin",

    [Parameter(Mandatory = $false)]
    [string]$Email = "admin@kba-framework.com",

    [Parameter(Mandatory = $false)]
    [SecureString]$Password,

    [Parameter(Mandatory = $false)]
    [string]$FirstName = "Admin",

    [Parameter(Mandatory = $false)]
    [string]$LastName = "System",

    [Parameter(Mandatory = $false)]
    [switch]$SkipTests
)

# Fonction pour afficher des messages colorés
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Type = "Info"
    )
    
    switch ($Type) {
        "Success" { Write-Host $Message -ForegroundColor Green }
        "Warning" { Write-Host $Message -ForegroundColor Yellow }
        "Error" { Write-Host $Message -ForegroundColor Red }
        "Info" { Write-Host $Message -ForegroundColor Cyan }
        default { Write-Host $Message }
    }
}

# Fonction pour gérer les erreurs d'API
function Test-ApiConnection {
    param([string]$Url)
    
    try {
        $response = Invoke-RestMethod -Uri "$Url/api/init/status" -Method Get -TimeoutSec 5
        return $true
    }
    catch {
        return $false
    }
}

# En-tête
Write-Host ""
Write-Host "╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     KBA Framework - Initialisation du premier admin       ║" -ForegroundColor Cyan
Write-Host "╚═══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Vérifier la connexion à l'API
Write-ColorOutput "⏳ Vérification de la connexion à l'API..." "Info"
if (-not (Test-ApiConnection -Url $BaseUrl)) {
    Write-ColorOutput "❌ Impossible de se connecter à l'API sur $BaseUrl" "Error"
    Write-ColorOutput "   Assurez-vous que l'application est en cours d'exécution." "Warning"
    Write-Host ""
    Write-Host "   Pour démarrer l'application, exécutez:"
    Write-Host "   dotnet run --project src/KBA.Framework.Api" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}
Write-ColorOutput "✓ Connexion réussie à l'API" "Success"
Write-Host ""

# Vérifier le statut d'initialisation
Write-ColorOutput "⏳ Vérification du statut d'initialisation..." "Info"
try {
    $statusResponse = Invoke-RestMethod -Uri "$BaseUrl/api/init/status" -Method Get
    
    if (-not $statusResponse.needsInitialization) {
        Write-ColorOutput "⚠️  Le système est déjà initialisé!" "Warning"
        Write-ColorOutput "   Nombre d'utilisateurs existants: $($statusResponse.userCount)" "Info"
        Write-Host ""
        Write-Host "   Pour vous connecter, utilisez: POST $BaseUrl/api/auth/login"
        Write-Host ""
        
        $continue = Read-Host "Voulez-vous quand même continuer? (O/N)"
        if ($continue -ne "O" -and $continue -ne "o") {
            exit 0
        }
    }
    else {
        Write-ColorOutput "✓ Le système nécessite une initialisation" "Success"
    }
}
catch {
    Write-ColorOutput "❌ Erreur lors de la vérification du statut: $($_.Exception.Message)" "Error"
    exit 1
}
Write-Host ""

# Demander le mot de passe si non fourni
if (-not $Password) {
    Write-ColorOutput "📝 Création du premier utilisateur administrateur" "Info"
    Write-Host ""
    Write-Host "   Nom d'utilisateur: $UserName"
    Write-Host "   Email: $Email"
    Write-Host "   Prénom: $FirstName"
    Write-Host "   Nom: $LastName"
    Write-Host ""
    
    $Password = Read-Host "   Mot de passe" -AsSecureString
    $PasswordConfirm = Read-Host "   Confirmer le mot de passe" -AsSecureString
    
    # Convertir en texte pour comparer
    $pwd1 = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password))
    $pwd2 = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($PasswordConfirm))
    
    if ($pwd1 -ne $pwd2) {
        Write-ColorOutput "❌ Les mots de passe ne correspondent pas!" "Error"
        exit 1
    }
    
    # Vérifier la complexité du mot de passe
    if ($pwd1.Length -lt 8) {
        Write-ColorOutput "❌ Le mot de passe doit contenir au moins 8 caractères!" "Error"
        exit 1
    }
    
    $PasswordPlain = $pwd1
}
else {
    $PasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Password))
}

Write-Host ""

# Créer l'utilisateur administrateur
Write-ColorOutput "⏳ Création de l'utilisateur administrateur..." "Info"

$adminUser = @{
    userName    = $UserName
    email       = $Email
    password    = $PasswordPlain
    firstName   = $FirstName
    lastName    = $LastName
    phoneNumber = $null
}

try {
    $createResponse = Invoke-RestMethod -Uri "$BaseUrl/api/init/first-admin" `
        -Method Post `
        -ContentType "application/json" `
        -Body ($adminUser | ConvertTo-Json)
    
    Write-ColorOutput "✓ Utilisateur administrateur créé avec succès!" "Success"
    Write-Host ""
    Write-Host "   ID: $($createResponse.user.id)"
    Write-Host "   Nom d'utilisateur: $($createResponse.user.userName)"
    Write-Host "   Email: $($createResponse.user.email)"
    Write-Host ""
}
catch {
    Write-ColorOutput "❌ Erreur lors de la création de l'utilisateur" "Error"
    
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $reader.BaseStream.Position = 0
        $responseBody = $reader.ReadToEnd()
        Write-Host "   Détails: $responseBody" -ForegroundColor Red
    }
    else {
        Write-Host "   Détails: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "   Vérifiez que le mot de passe respecte les critères:"
    Write-Host "   - Au moins 8 caractères"
    Write-Host "   - Au moins une majuscule"
    Write-Host "   - Au moins une minuscule"
    Write-Host "   - Au moins un chiffre"
    Write-Host "   - Au moins un caractère spécial"
    Write-Host ""
    exit 1
}

# Tester la connexion
if (-not $SkipTests) {
    Write-ColorOutput "⏳ Test de connexion..." "Info"
    
    $loginData = @{
        userName = $UserName
        password = $PasswordPlain
    }
    
    try {
        $loginResponse = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" `
            -Method Post `
            -ContentType "application/json" `
            -Body ($loginData | ConvertTo-Json)
        
        Write-ColorOutput "✓ Connexion réussie!" "Success"
        Write-Host ""
        Write-Host "   Token JWT obtenu:"
        Write-Host "   $($loginResponse.token.Substring(0, [Math]::Min(50, $loginResponse.token.Length)))..." -ForegroundColor DarkGray
        Write-Host ""
        
        # Tester un endpoint protégé
        Write-ColorOutput "⏳ Test d'un endpoint protégé..." "Info"
        
        $headers = @{
            "Authorization" = "Bearer $($loginResponse.token)"
        }
        
        try {
            $usersResponse = Invoke-RestMethod -Uri "$BaseUrl/api/users" `
                -Method Get `
                -Headers $headers
            
            Write-ColorOutput "✓ Accès aux endpoints protégés confirmé!" "Success"
            Write-Host "   Nombre d'utilisateurs: $($usersResponse.Count)"
        }
        catch {
            Write-ColorOutput "⚠️  Avertissement: Impossible d'accéder aux endpoints protégés" "Warning"
        }
    }
    catch {
        Write-ColorOutput "⚠️  Avertissement: Impossible de se connecter avec les identifiants fournis" "Warning"
        Write-Host "   Vous pourrez vous connecter manuellement plus tard."
    }
}

# Résumé final
Write-Host ""
Write-Host "╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║              ✓ Initialisation terminée!                   ║" -ForegroundColor Green
Write-Host "╚═══════════════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-ColorOutput "🎉 Le système KBA Framework est maintenant prêt à l'emploi!" "Success"
Write-Host ""
Write-Host "📚 Prochaines étapes:"
Write-Host "   1. Accédez à la documentation: $BaseUrl/api-docs"
Write-Host "   2. Explorez l'API: $BaseUrl/swagger"
Write-Host "   3. Connectez-vous avec:"
Write-Host "      - Nom d'utilisateur: $UserName"
Write-Host "      - Mot de passe: [le mot de passe que vous avez défini]"
Write-Host ""
Write-Host "💡 Conseils:"
Write-Host "   - Changez le mot de passe par défaut dès que possible"
Write-Host "   - Créez d'autres utilisateurs pour séparer les responsabilités"
Write-Host "   - Consultez la documentation complète dans docs/INITIALIZATION-GUIDE.md"
Write-Host ""
Write-ColorOutput "Merci d'utiliser KBA Framework!" "Info"
Write-Host ""
