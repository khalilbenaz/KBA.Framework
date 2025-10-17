# Script pour démarrer tous les microservices
Write-Host "🚀 Démarrage de l'architecture microservices KBA Framework" -ForegroundColor Green
Write-Host ""

# Fonction pour démarrer un service dans un nouveau terminal
function Start-Service {
    param(
        [string]$ServiceName,
        [string]$ServicePath,
        [int]$Port
    )
    
    Write-Host "▶️  Démarrage de $ServiceName (Port: $Port)..." -ForegroundColor Cyan
    
    $scriptBlock = "cd '$ServicePath'; dotnet run; Read-Host 'Appuyez sur Entrée pour fermer'"
    Start-Process pwsh -ArgumentList "-NoExit", "-Command", $scriptBlock
    
    Start-Sleep -Seconds 2
}

# Démarrer les services
$rootPath = $PSScriptRoot

Write-Host "📊 Services à démarrer:" -ForegroundColor Yellow
Write-Host "  1. Identity Service (Port 5001)"
Write-Host "  2. Product Service (Port 5002)"
Write-Host "  3. Tenant Service (Port 5003)"
Write-Host "  4. Permission Service (Port 5004)"
Write-Host "  5. API Gateway (Port 5000)"
Write-Host ""

Start-Service -ServiceName "Identity Service" -ServicePath "$rootPath\KBA.IdentityService" -Port 5001
Start-Service -ServiceName "Product Service" -ServicePath "$rootPath\KBA.ProductService" -Port 5002
Start-Service -ServiceName "Tenant Service" -ServicePath "$rootPath\KBA.TenantService" -Port 5003
Start-Service -ServiceName "Permission Service" -ServicePath "$rootPath\KBA.PermissionService" -Port 5004
Start-Service -ServiceName "API Gateway" -ServicePath "$rootPath\KBA.ApiGateway" -Port 5000

Write-Host ""
Write-Host "✅ Tous les services ont été démarrés!" -ForegroundColor Green
Write-Host ""
Write-Host "📡 Accès aux services:" -ForegroundColor Yellow
Write-Host "  - API Gateway:        http://localhost:5000" -ForegroundColor White
Write-Host "  - Identity Service:   http://localhost:5001/swagger" -ForegroundColor White
Write-Host "  - Product Service:    http://localhost:5002/swagger" -ForegroundColor White
Write-Host "  - Tenant Service:     http://localhost:5003/swagger" -ForegroundColor White
Write-Host "  - Permission Service: http://localhost:5004/swagger" -ForegroundColor White
Write-Host ""
Write-Host "💡 Utilisez l'API Gateway comme point d'entrée principal" -ForegroundColor Cyan
Write-Host "   URL: http://localhost:5000/swagger" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
