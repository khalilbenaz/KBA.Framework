# 🧪 Tester les Nouvelles Fonctionnalités - Guide Rapide

## ⚡ Démarrage Ultra-Rapide (3 minutes)

### Étape 1 : Démarrer les Services (30 secondes)

```powershell
cd microservices
.\start-microservices.ps1
```

**Attendez que vous voyiez** :
```
✅ Identity Service started on http://localhost:5001
✅ Product Service started on http://localhost:5002
✅ Tenant Service started on http://localhost:5003
✅ API Gateway started on http://localhost:5000
```

### Étape 2 : Lancer les Tests Automatisés (1 minute)

```powershell
.\test-product-service.ps1
```

**Vous verrez** :
```
✅ Health Check
✅ Get Categories
✅ Create Product: iPhone 15 Pro
✅ Search 'iPhone'
✅ Filter by price (1000-2000)
✅ Pagination (Page 1, Size 2)
✅ Sort by price (ascending)
✅ Get by SKU
🎉 Tests terminés avec succès!
```

### Étape 3 : Tester avec Swagger (1 minute)

Ouvrez votre navigateur :  
👉 **http://localhost:5002/swagger**

---

## 🎯 Tests Manuels dans Swagger

### Test 1 : Recherche Simple

1. Cliquez sur **GET /api/products/search**
2. Cliquez sur **Try it out**
3. Dans **searchTerm**, tapez : `iPhone`
4. Cliquez sur **Execute**

**Résultat attendu** :
```json
{
  "items": [
    {
      "id": "...",
      "name": "iPhone 15 Pro",
      "price": 1299.99,
      "stock": 50,
      "isAvailable": true
    }
  ],
  "totalCount": 1,
  "pageNumber": 1,
  "pageSize": 10
}
```

### Test 2 : Filtrage par Prix

1. **GET /api/products/search**
2. **Try it out**
3. Remplir :
   - `minPrice`: 500
   - `maxPrice`: 1500
4. **Execute**

**Résultat** : Tous les produits entre 500€ et 1500€

### Test 3 : Filtrage par Catégorie

1. **GET /api/products/categories** → Voir les catégories disponibles
2. **GET /api/products/search**
3. **Try it out**
4. `category`: `Électronique`
5. **Execute**

**Résultat** : Uniquement les produits de la catégorie Électronique

### Test 4 : Pagination

1. **GET /api/products/search**
2. **Try it out**
3. Remplir :
   - `pageNumber`: 1
   - `pageSize`: 2
4. **Execute**

**Observez** :
```json
{
  "items": [ ... ], // 2 produits
  "totalCount": 5,
  "pageNumber": 1,
  "pageSize": 2,
  "totalPages": 3,
  "hasNextPage": true
}
```

5. Changez `pageNumber` à `2` → Voir la page suivante

### Test 5 : Tri

1. **GET /api/products/search**
2. **Try it out**
3. Remplir :
   - `sortBy`: `price`
   - `sortDescending`: `false` (du moins cher au plus cher)
4. **Execute**

**Résultat** : Produits triés par prix croissant

### Test 6 : Recherche par SKU

1. **GET /api/products/sku/{sku}**
2. **Try it out**
3. `sku`: `IPHONE-15-PRO-001`
4. **Execute**

**Résultat** : Le produit correspondant au SKU

### Test 7 : Produits en Stock

1. **GET /api/products/search**
2. **Try it out**
3. `inStock`: `true`
4. **Execute**

**Résultat** : Uniquement les produits disponibles (stock > 0)

### Test 8 : Recherche Combinée

1. **GET /api/products/search**
2. **Try it out**
3. Remplir :
   - `searchTerm`: `Pro`
   - `minPrice`: 1000
   - `maxPrice`: 3000
   - `inStock`: `true`
   - `sortBy`: `price`
   - `pageSize`: 5
4. **Execute**

**Résultat** : Produits "Pro", entre 1000-3000€, en stock, triés par prix

---

## 🔬 Tests Avancés avec curl

### Recherche avec tous les filtres

```powershell
curl "http://localhost:5002/api/products/search?searchTerm=Pro&category=Informatique&minPrice=1000&maxPrice=3000&inStock=true&sortBy=price&sortDescending=false&pageNumber=1&pageSize=5"
```

### Obtenir les catégories

```powershell
curl http://localhost:5002/api/products/categories
```

### Recherche par SKU

```powershell
curl http://localhost:5002/api/products/sku/MACBOOK-PRO-16-001
```

### Créer un produit (avec PowerShell)

```powershell
$product = @{
    name = "Test Product"
    description = "Description test"
    sku = "TEST-001"
    price = 99.99
    stock = 10
    category = "Test"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5002/api/products" `
    -Method Post `
    -Body $product `
    -ContentType "application/json"
```

---

## 📊 Scénarios de Test Réels

### Scénario 1 : E-commerce Search

**Objectif** : Simuler une recherche client sur un site e-commerce

1. **Rechercher** "phone"
2. **Filtrer** par catégorie "Électronique"
3. **Filtrer** prix 500-1500€
4. **Filtrer** seulement en stock
5. **Trier** par prix croissant
6. **Paginer** 10 par page

**Requête** :
```
GET /api/products/search?searchTerm=phone&category=Électronique&minPrice=500&maxPrice=1500&inStock=true&sortBy=price&pageSize=10
```

### Scénario 2 : Admin Dashboard

**Objectif** : Afficher les produits par catégorie

1. **Obtenir** toutes les catégories :
   ```
   GET /api/products/categories
   ```

2. **Pour chaque catégorie**, obtenir les produits :
   ```
   GET /api/products/search?category=Électronique&pageSize=20
   ```

3. **Compter** les produits en rupture :
   ```
   GET /api/products/search?inStock=false
   ```

### Scénario 3 : Inventory Management

**Objectif** : Gérer le stock

1. **Rechercher** produits en rupture :
   ```
   GET /api/products/search?inStock=false
   ```

2. **Obtenir** un produit par SKU :
   ```
   GET /api/products/sku/DELL-XPS-15-001
   ```

3. **Mettre à jour** le stock (nécessite auth) :
   ```
   PATCH /api/products/{id}/stock
   Body: 50
   ```

---

## ✅ Checklist de Test

Cochez au fur et à mesure :

### Recherche
- [ ] Recherche simple (par terme)
- [ ] Recherche avec résultats vides
- [ ] Recherche insensible à la casse

### Filtrage
- [ ] Filtre par prix minimum
- [ ] Filtre par prix maximum
- [ ] Filtre par prix (min et max)
- [ ] Filtre par catégorie
- [ ] Filtre par disponibilité (en stock)
- [ ] Filtre par disponibilité (rupture)
- [ ] Combinaison de plusieurs filtres

### Pagination
- [ ] Page 1
- [ ] Page suivante
- [ ] Dernière page
- [ ] Taille de page personnalisée (2, 5, 10, 20)
- [ ] Métadonnées correctes (totalCount, totalPages, etc.)

### Tri
- [ ] Tri par nom (A-Z)
- [ ] Tri par nom (Z-A)
- [ ] Tri par prix (croissant)
- [ ] Tri par prix (décroissant)
- [ ] Tri par stock
- [ ] Tri par date de création

### Endpoints Spéciaux
- [ ] Obtenir par SKU
- [ ] Liste des catégories
- [ ] Health check

### Performance
- [ ] Recherche avec 100+ produits
- [ ] Pagination efficace (pas de timeout)
- [ ] Filtres rapides

---

## 🐛 Troubleshooting

### Les services ne démarrent pas

```powershell
# Vérifier les ports
netstat -an | findstr "5000 5001 5002 5003"

# Tuer les processus
Get-Process | Where-Object {$_.Name -eq "dotnet"} | Stop-Process

# Redémarrer
.\start-microservices.ps1
```

### Erreur "404 Not Found"

Vérifiez que vous utilisez le bon port :
- **Direct** : http://localhost:5002
- **Via Gateway** : http://localhost:5000

### Pas de produits dans les résultats

Lancez d'abord le script de test pour créer des données :
```powershell
.\test-product-service.ps1
```

### Erreur de compilation

```powershell
cd microservices
dotnet clean
dotnet restore
dotnet build
```

---

## 📈 Résultats Attendus

Après avoir testé toutes les fonctionnalités, vous devriez voir :

✅ **Recherche** : Rapide et pertinente  
✅ **Filtrage** : Précis et combinable  
✅ **Pagination** : Fluide avec métadonnées  
✅ **Tri** : Multi-critères fonctionnel  
✅ **Performance** : Pas de latence notable  
✅ **Logs** : Visibles dans la console  

---

## 🎉 Félicitations !

Si tous les tests passent, votre architecture microservices est maintenant :

- ✅ Fonctionnelle avec recherche avancée
- ✅ Performante avec pagination
- ✅ Flexible avec filtrage multi-critères
- ✅ Traçable avec logging structuré
- ✅ Robuste avec gestion d'erreurs
- ✅ Documentée avec Swagger
- ✅ Testable avec scripts automatisés

---

## 📞 Support

- **Documentation complète** : `AMELIORATIONS.md`
- **Swagger UI** : http://localhost:5002/swagger
- **Logs** : `microservices/KBA.ProductService/logs/`

**Bon test ! 🚀**
