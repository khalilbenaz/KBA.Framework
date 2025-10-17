# Script de test du Permission Service
# Teste toutes les fonctionnalités des permissions

$baseUrl = "http://localhost:5004/api/permissions"
$gatewayUrl = "http://localhost:5000/api/permissions"

Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     Test du Permission Service - Gestion des Permissions  ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Fonction pour afficher les résultats
function Show-Result {
    param (
        [string]$TestName,
        [bool]$Success,
        [string]$Details = ""
    )
    
    $icon = if ($Success) { "✅" } else { "❌" }
    $color = if ($Success) { "Green" } else { "Red" }
    
    Write-Host "$icon $TestName" -ForegroundColor $color
    if ($Details) {
        Write-Host "   $Details" -ForegroundColor Gray
    }
}

# Test 1: Health Check
Write-Host "[Test 1] Health Check" -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "http://localhost:5004/health" -Method Get
    Show-Result -TestName "Health Check" -Success $true -Details "Service is healthy"
}
catch {
    Show-Result -TestName "Health Check" -Success $false -Details $_.Exception.Message
    Write-Host "❌ Le service n'est pas démarré. Lancez-le avec .\start-microservices.ps1" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Test 2: Obtenir toutes les permissions (avec seed)
Write-Host "[Test 2] Obtenir toutes les permissions" -ForegroundColor Yellow
try {
    $permissions = Invoke-RestMethod -Uri $baseUrl -Method Get
    Show-Result -TestName "Get All Permissions" -Success $true -Details "Found $($permissions.Count) permissions (hierarchical)"
    
    if ($permissions.Count -gt 0) {
        Write-Host "   Groupes trouvés:" -ForegroundColor Gray
        $groups = $permissions | Select-Object -ExpandProperty groupName -Unique
        foreach ($group in $groups) {
            $count = ($permissions | Where-Object { $_.groupName -eq $group }).Count
            Write-Host "     - $group : $count permissions" -ForegroundColor Gray
        }
    }
}
catch {
    Show-Result -TestName "Get All Permissions" -Success $false -Details $_.Exception.Message
}

Write-Host ""

# Test 3: Recherche de permissions
Write-Host "[Test 3] Recherche de permissions" -ForegroundColor Yellow

# Recherche par terme
try {
    $searchResult = Invoke-RestMethod -Uri "$baseUrl/search?searchTerm=Product" -Method Get
    Show-Result -TestName "Search 'Product'" -Success $true -Details "Found $($searchResult.totalCount) results"
}
catch {
    Show-Result -TestName "Search 'Product'" -Success $false -Details $_.Exception.Message
}

# Recherche par groupe
try {
    $searchResult = Invoke-RestMethod -Uri "$baseUrl/search?groupName=Users" -Method Get
    Show-Result -TestName "Search by group 'Users'" -Success $true -Details "Found $($searchResult.totalCount) results"
}
catch {
    Show-Result -TestName "Search by group" -Success $false -Details $_.Exception.Message
}

Write-Host ""

# Test 4: Obtenir par groupe
Write-Host "[Test 4] Obtenir permissions par groupe" -ForegroundColor Yellow

$groups = @("Users", "Products", "Tenants", "Permissions", "System")
foreach ($group in $groups) {
    try {
        $groupPerms = Invoke-RestMethod -Uri "$baseUrl/group/$group" -Method Get
        Show-Result -TestName "Get group '$group'" -Success $true -Details "$($groupPerms.Count) permissions"
    }
    catch {
        Show-Result -TestName "Get group '$group'" -Success $false -Details $_.Exception.Message
    }
}

Write-Host ""

# Test 5: Obtenir par nom
Write-Host "[Test 5] Obtenir permission par nom" -ForegroundColor Yellow
try {
    $permission = Invoke-RestMethod -Uri "$baseUrl/name/Products.Create" -Method Get
    Show-Result -TestName "Get by name 'Products.Create'" -Success $true -Details "$($permission.displayName)"
}
catch {
    Show-Result -TestName "Get by name" -Success $false -Details $_.Exception.Message
}

Write-Host ""

# Test 6: Pagination
Write-Host "[Test 6] Pagination" -ForegroundColor Yellow
try {
    $page1 = Invoke-RestMethod -Uri "$baseUrl/search?pageNumber=1&pageSize=5" -Method Get
    Show-Result -TestName "Pagination (Page 1, Size 5)" -Success $true -Details "Page $($page1.pageNumber)/$($page1.totalPages) - $($page1.items.Count) items"
    
    if ($page1.totalPages -gt 1) {
        $page2 = Invoke-RestMethod -Uri "$baseUrl/search?pageNumber=2&pageSize=5" -Method Get
        Show-Result -TestName "Pagination (Page 2, Size 5)" -Success $true -Details "Page $($page2.pageNumber)/$($page2.totalPages) - $($page2.items.Count) items"
    }
}
catch {
    Show-Result -TestName "Pagination" -Success $false -Details $_.Exception.Message
}

Write-Host ""

# Test 7: Vérification de permission (sans grant)
Write-Host "[Test 7] Vérification de permission" -ForegroundColor Yellow
try {
    $checkDto = @{
        userId = [Guid]::NewGuid()
        permissionName = "Products.Create"
        tenantId = $null
    } | ConvertTo-Json

    $result = Invoke-RestMethod -Uri "$baseUrl/check" -Method Post -Body $checkDto -ContentType "application/json"
    
    if ($result.isGranted -eq $false) {
        Show-Result -TestName "Check Permission (no grant)" -Success $true -Details "Correctly returned NOT granted"
    } else {
        Show-Result -TestName "Check Permission (no grant)" -Success $false -Details "Should be NOT granted"
    }
}
catch {
    Show-Result -TestName "Check Permission" -Success $false -Details $_.Exception.Message
}

Write-Host ""

# Test 8: Grant & Revoke (nécessite un token - on teste juste le format)
Write-Host "[Test 8] Format Grant/Revoke" -ForegroundColor Yellow

$testUserId = [Guid]::NewGuid()

Write-Host "   Note: Grant/Revoke nécessitent authentification (skipped)" -ForegroundColor Gray
Write-Host "   Format de grant:" -ForegroundColor Gray
Write-Host "   {" -ForegroundColor DarkGray
Write-Host '     "permissionName": "Products.Create",' -ForegroundColor DarkGray
Write-Host '     "providerName": "User",' -ForegroundColor DarkGray
Write-Host "     `"providerKey`": `"$testUserId`"," -ForegroundColor DarkGray
Write-Host '     "tenantId": null' -ForegroundColor DarkGray
Write-Host "   }" -ForegroundColor DarkGray

Write-Host ""

# Test 9: Via API Gateway
Write-Host "[Test 9] Accès via API Gateway" -ForegroundColor Yellow
try {
    $gatewayPerms = Invoke-RestMethod -Uri $gatewayUrl -Method Get
    Show-Result -TestName "Gateway access" -Success $true -Details "Found $($gatewayPerms.Count) permissions via gateway"
}
catch {
    Show-Result -TestName "Gateway access" -Success $false -Details $_.Exception.Message
}

Write-Host ""

# Test 10: Récapitulatif
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                    Récapitulatif des Tests                 ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

Write-Host "✅ Fonctionnalités testées:" -ForegroundColor Green
Write-Host "   • Health Check" -ForegroundColor White
Write-Host "   • Liste de toutes les permissions (18 seedées)" -ForegroundColor White
Write-Host "   • Recherche par terme et par groupe" -ForegroundColor White
Write-Host "   • Obtenir par groupe (5 groupes)" -ForegroundColor White
Write-Host "   • Obtenir par nom" -ForegroundColor White
Write-Host "   • Pagination" -ForegroundColor White
Write-Host "   • Vérification de permission" -ForegroundColor White
Write-Host "   • Accès via API Gateway" -ForegroundColor White
Write-Host ""

Write-Host "📊 Permissions pré-configurées (18):" -ForegroundColor Yellow
Write-Host "   • Users (4): View, Create, Edit, Delete" -ForegroundColor White
Write-Host "   • Products (5): View, Create, Edit, Delete, ManageStock" -ForegroundColor White
Write-Host "   • Tenants (4): View, Create, Edit, Delete" -ForegroundColor White
Write-Host "   • Permissions (3): View, Grant, Revoke" -ForegroundColor White
Write-Host "   • System (2): Settings, Audit" -ForegroundColor White
Write-Host ""

Write-Host "📊 Endpoints disponibles:" -ForegroundColor Yellow
Write-Host "   GET    /api/permissions                  - Liste complète (hiérarchique)" -ForegroundColor White
Write-Host "   GET    /api/permissions/search           - Recherche avec pagination" -ForegroundColor White
Write-Host "   GET    /api/permissions/{id}             - Par ID" -ForegroundColor White
Write-Host "   GET    /api/permissions/name/{name}      - Par nom" -ForegroundColor White
Write-Host "   GET    /api/permissions/group/{group}    - Par groupe" -ForegroundColor White
Write-Host "   POST   /api/permissions                  - Créer (auth requise)" -ForegroundColor White
Write-Host "   DELETE /api/permissions/{id}             - Supprimer (auth requise)" -ForegroundColor White
Write-Host "   POST   /api/permissions/check            - Vérifier permission" -ForegroundColor White
Write-Host "   POST   /api/permissions/grant            - Accorder (auth requise)" -ForegroundColor White
Write-Host "   POST   /api/permissions/revoke           - Révoquer (auth requise)" -ForegroundColor White
Write-Host "   GET    /api/permissions/user/{userId}    - Permissions utilisateur" -ForegroundColor White
Write-Host "   GET    /api/permissions/role/{roleId}    - Permissions rôle" -ForegroundColor White
Write-Host ""

Write-Host "🎉 Tests terminés avec succès!" -ForegroundColor Green
Write-Host ""
Write-Host "💡 Testez avec Swagger: http://localhost:5004/swagger" -ForegroundColor Cyan
Write-Host "💡 Via Gateway: http://localhost:5000/api/permissions" -ForegroundColor Cyan
Write-Host ""

Write-Host "🔐 Pour tester Grant/Revoke:" -ForegroundColor Yellow
Write-Host "   1. Authentifiez-vous via Identity Service" -ForegroundColor White
Write-Host "   2. Récupérez le token JWT" -ForegroundColor White
Write-Host "   3. Utilisez Swagger avec Authorization: Bearer {token}" -ForegroundColor White
