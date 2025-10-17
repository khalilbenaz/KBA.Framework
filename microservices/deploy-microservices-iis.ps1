# Script de déploiement des microservices sur IIS
# Exécuter en tant qu'Administrateur

param(
    [string]$Environment = "Production",
    [string]$IISBasePath = "C:\inetpub\wwwroot"
)

Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║   Déploiement Microservices KBA Framework sur IIS         ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Vérifier les droits admin
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "❌ Ce script nécessite des droits administrateur!" -ForegroundColor Red
    Write-Host "   Relancez PowerShell en tant qu'Administrateur" -ForegroundColor Yellow
    exit 1
}

# Configuration des microservices
$services = @(
    @{
        Name = "IdentityService"
        Port = 5001
        Path = "KBA.IdentityService"
        DisplayName = "KBA Identity Service"
    },
    @{
        Name = "ProductService"
        Port = 5002
        Path = "KBA.ProductService"
        DisplayName = "KBA Product Service"
    },
    @{
        Name = "TenantService"
        Port = 5003
        Path = "KBA.TenantService"
        DisplayName = "KBA Tenant Service"
    },
    @{
        Name = "ApiGateway"
        Port = 5000
        Path = "KBA.ApiGateway"
        DisplayName = "KBA API Gateway"
    }
)

# Importer le module WebAdministration
Import-Module WebAdministration -ErrorAction SilentlyContinue
if ($? -eq $false) {
    Write-Host "❌ Le module WebAdministration n'est pas disponible" -ForegroundColor Red
    Write-Host "   Installez IIS et ses outils de gestion" -ForegroundColor Yellow
    exit 1
}

Write-Host "📋 Services à déployer:" -ForegroundColor Yellow
foreach ($service in $services) {
    Write-Host "   • $($service.DisplayName) (Port $($service.Port))" -ForegroundColor White
}
Write-Host ""

foreach ($service in $services) {
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
    Write-Host "▶️  Déploiement de $($service.DisplayName)" -ForegroundColor Cyan
    Write-Host ""
    
    $projectPath = Join-Path $PSScriptRoot $service.Path
    $publishPath = Join-Path $IISBasePath "KBA_$($service.Name)"
    $poolName = "KBA_$($service.Name)Pool"
    $siteName = "KBA_$($service.Name)"
    
    # 1. Publication
    Write-Host "   [1/5] Publication du projet..." -ForegroundColor Yellow
    $projectFile = Join-Path $projectPath "$($service.Path).csproj"
    
    if (-not (Test-Path $projectFile)) {
        Write-Host "   ⚠️  Projet non trouvé: $projectFile" -ForegroundColor Red
        continue
    }
    
    dotnet publish $projectFile `
        -c $Environment `
        -o $publishPath `
        --nologo `
        --verbosity quiet
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "   ❌ Échec de la publication" -ForegroundColor Red
        continue
    }
    Write-Host "   ✅ Publication réussie" -ForegroundColor Green
    
    # 2. Création du pool d'application
    Write-Host "   [2/5] Configuration du pool d'application..." -ForegroundColor Yellow
    
    if (Test-Path "IIS:\AppPools\$poolName") {
        Remove-WebAppPool -Name $poolName
    }
    
    New-WebAppPool -Name $poolName | Out-Null
    Set-ItemProperty "IIS:\AppPools\$poolName" -Name "managedRuntimeVersion" -Value ""
    Set-ItemProperty "IIS:\AppPools\$poolName" -Name "startMode" -Value "AlwaysRunning"
    Set-ItemProperty "IIS:\AppPools\$poolName" -Name "processModel" -Value @{idleTimeout="00:00:00"}
    
    Write-Host "   ✅ Pool créé: $poolName" -ForegroundColor Green
    
    # 3. Création/Mise à jour du site
    Write-Host "   [3/5] Configuration du site IIS..." -ForegroundColor Yellow
    
    if (Get-Website -Name $siteName -ErrorAction SilentlyContinue) {
        Remove-Website -Name $siteName
    }
    
    New-Website -Name $siteName `
        -PhysicalPath $publishPath `
        -ApplicationPool $poolName `
        -Port $service.Port `
        -Force | Out-Null
    
    Write-Host "   ✅ Site créé: $siteName sur port $($service.Port)" -ForegroundColor Green
    
    # 4. Configuration des permissions
    Write-Host "   [4/5] Configuration des permissions..." -ForegroundColor Yellow
    
    $acl = Get-Acl $publishPath
    $identity = "IIS AppPool\$poolName"
    $fileSystemRights = [System.Security.AccessControl.FileSystemRights]::Modify
    $inheritanceFlags = [System.Security.AccessControl.InheritanceFlags]"ContainerInherit,ObjectInherit"
    $propagationFlags = [System.Security.AccessControl.PropagationFlags]::None
    $accessControlType = [System.Security.AccessControl.AccessControlType]::Allow
    
    $rule = New-Object System.Security.AccessControl.FileSystemAccessRule(
        $identity,
        $fileSystemRights,
        $inheritanceFlags,
        $propagationFlags,
        $accessControlType
    )
    
    $acl.SetAccessRule($rule)
    Set-Acl $publishPath $acl
    
    Write-Host "   ✅ Permissions configurées" -ForegroundColor Green
    
    # 5. Création du fichier web.config si nécessaire
    Write-Host "   [5/5] Vérification de web.config..." -ForegroundColor Yellow
    
    $webConfigPath = Join-Path $publishPath "web.config"
    if (-not (Test-Path $webConfigPath)) {
        $webConfigContent = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet"
                  arguments=".\$($service.Path).dll"
                  stdoutLogEnabled="true"
                  stdoutLogFile=".\logs\stdout"
                  hostingModel="inprocess">
        <environmentVariables>
          <environmentVariable name="ASPNETCORE_ENVIRONMENT" value="$Environment" />
        </environmentVariables>
      </aspNetCore>
    </system.webServer>
  </location>
</configuration>
"@
        $webConfigContent | Out-File -FilePath $webConfigPath -Encoding UTF8
        Write-Host "   ✅ web.config créé" -ForegroundColor Green
    } else {
        Write-Host "   ✅ web.config existe déjà" -ForegroundColor Green
    }
    
    # Démarrer le site
    Start-Website -Name $siteName
    Start-WebAppPool -Name $poolName
    
    Write-Host ""
    Write-Host "   ✅ $($service.DisplayName) déployé avec succès!" -ForegroundColor Green
    Write-Host "   🌐 URL: http://localhost:$($service.Port)" -ForegroundColor Cyan
    Write-Host ""
}

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host ""
Write-Host "✅ Déploiement terminé!" -ForegroundColor Green
Write-Host ""
Write-Host "📡 Accès aux services:" -ForegroundColor Yellow
Write-Host "   • API Gateway:      http://localhost:5000" -ForegroundColor White
Write-Host "   • Identity Service: http://localhost:5001/swagger" -ForegroundColor White
Write-Host "   • Product Service:  http://localhost:5002/swagger" -ForegroundColor White
Write-Host "   • Tenant Service:   http://localhost:5003/swagger" -ForegroundColor White
Write-Host ""
Write-Host "📝 Logs disponibles dans:" -ForegroundColor Yellow
foreach ($service in $services) {
    $logPath = Join-Path $IISBasePath "KBA_$($service.Name)\logs"
    Write-Host "   • $($service.DisplayName): $logPath" -ForegroundColor White
}
Write-Host ""
Write-Host "💡 Commandes utiles:" -ForegroundColor Yellow
Write-Host "   • Redémarrer tous les sites: Get-Website | Where-Object {`$_.Name -like 'KBA_*'} | Restart-Website" -ForegroundColor Gray
Write-Host "   • Voir les logs: Get-Content '$IISBasePath\KBA_ApiGateway\logs\stdout*.log' -Tail 50" -ForegroundColor Gray
Write-Host ""

# Tester les endpoints
Write-Host "🧪 Test des services..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

foreach ($service in $services) {
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:$($service.Port)/health" -TimeoutSec 5 -UseBasicParsing
        if ($response.StatusCode -eq 200) {
            Write-Host "   ✅ $($service.DisplayName) : OK" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "   ⚠️  $($service.DisplayName) : Non disponible (peut prendre quelques secondes)" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "🎉 Déploiement IIS terminé avec succès!" -ForegroundColor Green
