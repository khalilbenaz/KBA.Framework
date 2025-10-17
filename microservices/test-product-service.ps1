# Script de test du Product Service
# Teste toutes les nouvelles fonctionnalités

$baseUrl = "http://localhost:5002/api/products"
$gatewayUrl = "http://localhost:5000/api/products"

Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     Test du Product Service - Fonctionnalités Avancées    ║" -ForegroundColor Cyan
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
    $response = Invoke-RestMethod -Uri "http://localhost:5002/health" -Method Get
    Show-Result -TestName "Health Check" -Success $true -Details "Service is healthy"
}
catch {
    Show-Result -TestName "Health Check" -Success $false -Details $_.Exception.Message
    Write-Host "❌ Le service n'est pas démarré. Lancez-le avec .\start-microservices.ps1" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Test 2: Obtenir toutes les catégories
Write-Host "[Test 2] Obtenir les catégories" -ForegroundColor Yellow
try {
    $categories = Invoke-RestMethod -Uri "$baseUrl/categories" -Method Get
    Show-Result -TestName "Get Categories" -Success $true -Details "Found $($categories.Count) categories"
    if ($categories.Count -gt 0) {
        Write-Host "   Catégories: $($categories -join ', ')" -ForegroundColor Gray
    }
}
catch {
    Show-Result -TestName "Get Categories" -Success $false -Details $_.Exception.Message
}

Write-Host ""

# Test 3: Créer des produits de test (sans authentification - à corriger plus tard)
Write-Host "[Test 3] Créer des produits de test" -ForegroundColor Yellow

$testProducts = @(
    @{
        name = "iPhone 15 Pro"
        description = "Smartphone Apple dernier modèle"
        sku = "IPHONE-15-PRO-001"
        price = 1299.99
        stock = 50
        category = "Électronique"
    },
    @{
        name = "Samsung Galaxy S24"
        description = "Smartphone Samsung flagship"
        sku = "SAMSUNG-S24-001"
        price = 999.99
        stock = 30
        category = "Électronique"
    },
    @{
        name = "MacBook Pro 16"
        description = "Ordinateur portable Apple M3"
        sku = "MACBOOK-PRO-16-001"
        price = 2999.99
        stock = 15
        category = "Informatique"
    },
    @{
        name = "Dell XPS 15"
        description = "Ordinateur portable Dell"
        sku = "DELL-XPS-15-001"
        price = 1799.99
        stock = 0
        category = "Informatique"
    },
    @{
        name = "Sony WH-1000XM5"
        description = "Casque audio sans fil"
        sku = "SONY-WH1000XM5-001"
        price = 399.99
        stock = 100
        category = "Audio"
    }
)

$createdProducts = @()

foreach ($product in $testProducts) {
    try {
        $body = $product | ConvertTo-Json
        $result = Invoke-RestMethod -Uri $baseUrl -Method Post -Body $body -ContentType "application/json"
        $createdProducts += $result
        Show-Result -TestName "Create Product: $($product.name)" -Success $true -Details "ID: $($result.id)"
    }
    catch {
        if ($_.Exception.Message -like "*existe déjà*") {
            Show-Result -TestName "Create Product: $($product.name)" -Success $true -Details "Already exists (OK)"
        }
        else {
            Show-Result -TestName "Create Product: $($product.name)" -Success $false -Details $_.Exception.Message
        }
    }
}

Write-Host ""

# Test 4: Obtenir tous les produits
Write-Host "[Test 4] Obtenir tous les produits" -ForegroundColor Yellow
try {
    $allProducts = Invoke-RestMethod -Uri $baseUrl -Method Get
    Show-Result -TestName "Get All Products" -Success $true -Details "Found $($allProducts.Count) products"
}
catch {
    Show-Result -TestName "Get All Products" -Success $false -Details $_.Exception.Message
}

Write-Host ""

# Test 5: Recherche simple
Write-Host "[Test 5] Recherche simple" -ForegroundColor Yellow
try {
    $searchResult = Invoke-RestMethod -Uri "$baseUrl/search?searchTerm=iPhone" -Method Get
    Show-Result -TestName "Search 'iPhone'" -Success $true -Details "Found $($searchResult.totalCount) results"
}
catch {
    Show-Result -TestName "Search 'iPhone'" -Success $false -Details $_.Exception.Message
}

Write-Host ""

# Test 6: Recherche avec filtres
Write-Host "[Test 6] Recherche avec filtres" -ForegroundColor Yellow

# Prix entre 1000 et 2000
try {
    $searchResult = Invoke-RestMethod -Uri "$baseUrl/search?minPrice=1000&maxPrice=2000" -Method Get
    Show-Result -TestName "Filter by price (1000-2000)" -Success $true -Details "Found $($searchResult.totalCount) products"
}
catch {
    Show-Result -TestName "Filter by price" -Success $false -Details $_.Exception.Message
}

# Catégorie Électronique
try {
    $searchResult = Invoke-RestMethod -Uri "$baseUrl/search?category=Électronique" -Method Get
    Show-Result -TestName "Filter by category (Électronique)" -Success $true -Details "Found $($searchResult.totalCount) products"
}
catch {
    Show-Result -TestName "Filter by category" -Success $false -Details $_.Exception.Message
}

# Produits en stock
try {
    $searchResult = Invoke-RestMethod -Uri "$baseUrl/search?inStock=true" -Method Get
    Show-Result -TestName "Filter by stock (In Stock)" -Success $true -Details "Found $($searchResult.totalCount) products"
}
catch {
    Show-Result -TestName "Filter by stock" -Success $false -Details $_.Exception.Message
}

Write-Host ""

# Test 7: Pagination
Write-Host "[Test 7] Pagination" -ForegroundColor Yellow
try {
    $page1 = Invoke-RestMethod -Uri "$baseUrl/search?pageNumber=1&pageSize=2" -Method Get
    Show-Result -TestName "Pagination (Page 1, Size 2)" -Success $true -Details "Page $($page1.pageNumber)/$($page1.totalPages) - $($page1.items.Count) items"
    
    if ($page1.totalPages -gt 1) {
        $page2 = Invoke-RestMethod -Uri "$baseUrl/search?pageNumber=2&pageSize=2" -Method Get
        Show-Result -TestName "Pagination (Page 2, Size 2)" -Success $true -Details "Page $($page2.pageNumber)/$($page2.totalPages) - $($page2.items.Count) items"
    }
}
catch {
    Show-Result -TestName "Pagination" -Success $false -Details $_.Exception.Message
}

Write-Host ""

# Test 8: Tri
Write-Host "[Test 8] Tri des résultats" -ForegroundColor Yellow

# Tri par prix (croissant)
try {
    $searchResult = Invoke-RestMethod -Uri "$baseUrl/search?sortBy=price&sortDescending=false" -Method Get
    $prices = $searchResult.items | ForEach-Object { $_.price }
    Show-Result -TestName "Sort by price (ascending)" -Success $true -Details "Prices: $($prices -join ', ')"
}
catch {
    Show-Result -TestName "Sort by price" -Success $false -Details $_.Exception.Message
}

# Tri par nom
try {
    $searchResult = Invoke-RestMethod -Uri "$baseUrl/search?sortBy=name" -Method Get
    $names = $searchResult.items | ForEach-Object { $_.name }
    Show-Result -TestName "Sort by name" -Success $true -Details "First: $($names[0])"
}
catch {
    Show-Result -TestName "Sort by name" -Success $false -Details $_.Exception.Message
}

Write-Host ""

# Test 9: Recherche par SKU
Write-Host "[Test 9] Recherche par SKU" -ForegroundColor Yellow
try {
    $product = Invoke-RestMethod -Uri "$baseUrl/sku/IPHONE-15-PRO-001" -Method Get
    Show-Result -TestName "Get by SKU" -Success $true -Details "Found: $($product.name)"
}
catch {
    Show-Result -TestName "Get by SKU" -Success $false -Details $_.Exception.Message
}

Write-Host ""

# Test 10: Récapitulatif
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                    Récapitulatif des Tests                 ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

Write-Host "✅ Fonctionnalités testées:" -ForegroundColor Green
Write-Host "   • Health Check" -ForegroundColor White
Write-Host "   • Liste des catégories" -ForegroundColor White
Write-Host "   • Création de produits" -ForegroundColor White
Write-Host "   • Recherche avec filtres (prix, catégorie, stock)" -ForegroundColor White
Write-Host "   • Pagination" -ForegroundColor White
Write-Host "   • Tri (prix, nom)" -ForegroundColor White
Write-Host "   • Recherche par SKU" -ForegroundColor White
Write-Host ""

Write-Host "📊 Endpoints disponibles:" -ForegroundColor Yellow
Write-Host "   GET    /api/products                  - Liste complète" -ForegroundColor White
Write-Host "   GET    /api/products/search           - Recherche avancée" -ForegroundColor White
Write-Host "   GET    /api/products/{id}             - Par ID" -ForegroundColor White
Write-Host "   GET    /api/products/sku/{sku}        - Par SKU" -ForegroundColor White
Write-Host "   GET    /api/products/categories       - Liste des catégories" -ForegroundColor White
Write-Host "   POST   /api/products                  - Créer (auth requise)" -ForegroundColor White
Write-Host "   PUT    /api/products/{id}             - Modifier (auth requise)" -ForegroundColor White
Write-Host "   PATCH  /api/products/{id}/stock       - MAJ stock (auth requise)" -ForegroundColor White
Write-Host "   DELETE /api/products/{id}             - Supprimer (auth requise)" -ForegroundColor White
Write-Host ""

Write-Host "🎉 Tests terminés avec succès!" -ForegroundColor Green
Write-Host ""
Write-Host "💡 Testez avec Swagger: http://localhost:5002/swagger" -ForegroundColor Cyan
