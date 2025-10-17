# 📖 Comprendre les Fichiers Protocol Buffers (.proto)

## 🤔 C'est Quoi un Fichier .proto ?

Un fichier `.proto` est un **contrat de données** entre services qui utilise le langage **Protocol Buffers** (protobuf) de Google.

### Analogie Simple

Imaginez que vous voulez envoyer un colis à quelqu'un :
- **REST/JSON** = Vous écrivez une lettre (texte) avec toutes les informations
- **gRPC/Protobuf** = Vous remplissez un formulaire structuré (binaire, compact, rapide)

Le fichier `.proto` c'est le **modèle du formulaire** que tout le monde doit suivre !

## 🎯 Rôle des Fichiers .proto

### 1. **Définition du Contrat**

Le fichier `.proto` définit :
- Les **services** disponibles (comme une API)
- Les **méthodes** (comme les endpoints REST)
- Les **messages** (comme les DTOs/Models)

### 2. **Génération de Code Automatique**

À partir du fichier `.proto`, les outils génèrent **automatiquement** :
- Le code serveur (pour recevoir les appels)
- Le code client (pour faire les appels)
- Les classes de sérialisation/désérialisation

### 3. **Type Safety**

Le compilateur protobuf vérifie que :
- Les types sont corrects
- Les messages sont valides
- Les champs obligatoires sont présents

## 📝 Anatomie d'un Fichier .proto

### Structure de Base

```protobuf
syntax = "proto3";                              // Version de Protocol Buffers

option csharp_namespace = "MonNamespace";       // Namespace C# généré

package monservice;                             // Package protobuf

// Définition d'un SERVICE (comme un Controller)
service MonService {
  rpc MaMethode (MaRequete) returns (MaReponse);
}

// Définition d'un MESSAGE (comme un DTO)
message MaRequete {
  string champ1 = 1;    // = 1 est le numéro du champ (pas une valeur!)
  int32 champ2 = 2;
  optional string champ3 = 3;
}

message MaReponse {
  bool success = 1;
  string message = 2;
}
```

### Exemple Concret : permission.proto

```protobuf
syntax = "proto3";
option csharp_namespace = "KBA.Framework.Grpc.Permissions";
package permissions;

// ✅ SERVICE = Liste des opérations disponibles
service PermissionService {
  // Comme un endpoint : POST /api/permissions/check
  rpc CheckPermission (CheckPermissionRequest) returns (CheckPermissionResponse);
  
  // Comme un endpoint : GET /api/permissions/user/{id}
  rpc GetUserPermissions (GetUserPermissionsRequest) returns (GetUserPermissionsResponse);
}

// ✅ MESSAGE = Structure de données (DTO)
message CheckPermissionRequest {
  string user_id = 1;           // Champ #1
  string permission_name = 2;   // Champ #2
  optional string tenant_id = 3; // Champ #3 (optionnel)
  string correlation_id = 4;    // Champ #4
}

message CheckPermissionResponse {
  bool is_granted = 1;
  string permission_name = 2;
  string granted_by = 3;
}
```

## 🔧 Création : Manuelle ou Automatique ?

### ⚠️ **RÉPONSE : Manuelle !**

Les fichiers `.proto` doivent être **écrits manuellement** par vous, le développeur.

**Pourquoi ?**
- C'est vous qui définissez le contrat de votre API
- C'est comme définir une interface en C#
- Vous décidez quelles données sont nécessaires

### 🔄 Mais le Code est Généré Automatiquement !

Une fois le fichier `.proto` créé, les **classes C#** sont générées automatiquement lors de la compilation.

## 📊 Processus Complet

```
┌──────────────────────────────────────────────────────────┐
│  ÉTAPE 1 : Vous écrivez MANUELLEMENT                     │
│  ─────────────────────────────────────                   │
│  permission.proto                                         │
│  ┌───────────────────────────────────┐                   │
│  │ service PermissionService {       │                   │
│  │   rpc CheckPermission(...);       │                   │
│  │ }                                 │                   │
│  └───────────────────────────────────┘                   │
└──────────────────────────────────────────────────────────┘
                    ▼
┌──────────────────────────────────────────────────────────┐
│  ÉTAPE 2 : Outils gRPC génèrent AUTOMATIQUEMENT          │
│  ────────────────────────────────────────────            │
│  (lors de dotnet build)                                  │
│                                                           │
│  ✅ PermissionService.PermissionServiceClient (Client)   │
│  ✅ PermissionService.PermissionServiceBase (Serveur)    │
│  ✅ CheckPermissionRequest (classe C#)                   │
│  ✅ CheckPermissionResponse (classe C#)                  │
└──────────────────────────────────────────────────────────┘
                    ▼
┌──────────────────────────────────────────────────────────┐
│  ÉTAPE 3 : Vous UTILISEZ le code généré                  │
│  ────────────────────────────────────────                │
│  // Serveur                                              │
│  public class MyService : PermissionServiceBase {        │
│     public override async Task<Response> CheckPermission │
│       (Request req, ServerCallContext ctx) { }           │
│  }                                                        │
│                                                           │
│  // Client                                               │
│  var client = new PermissionServiceClient(channel);      │
│  var response = await client.CheckPermissionAsync(req);  │
└──────────────────────────────────────────────────────────┘
```

## 📦 Configuration dans le Projet

### Dans KBA.Framework.Grpc.csproj

```xml
<ItemGroup>
  <!-- Package pour générer le code C# -->
  <PackageReference Include="Grpc.Tools" Version="2.60.0">
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>
</ItemGroup>

<ItemGroup>
  <!-- IMPORTANT : Indique quels fichiers .proto compiler -->
  <Protobuf Include="Protos\**\*.proto" GrpcServices="Both" />
  <!--                                   ^^^^^^^^^^^^^^^^^^^^
       Both = Génère le code Client ET Serveur
       Client = Génère uniquement le client
       Server = Génère uniquement le serveur
  -->
</ItemGroup>
```

### Génération du Code

Le code est généré **automatiquement** quand vous :

```bash
dotnet build
```

Les fichiers générés se trouvent dans :
```
obj/Debug/net8.0/
  ├── Permission.cs          # Code généré depuis permission.proto
  ├── PermissionGrpc.cs      # Client et serveur gRPC
  └── ...
```

## 🎓 Types de Données Protobuf

### Types de Base

| Proto Type | C# Type | Description |
|------------|---------|-------------|
| `string` | `string` | Chaîne de caractères |
| `int32` | `int` | Entier 32-bit |
| `int64` | `long` | Entier 64-bit |
| `bool` | `bool` | Booléen |
| `double` | `double` | Nombre décimal |
| `bytes` | `byte[]` | Données binaires |

### Types Spéciaux

```protobuf
// ✅ Champ optionnel
optional string tenant_id = 1;

// ✅ Liste répétée
repeated string permissions = 2;

// ✅ Énumération
enum Status {
  UNKNOWN = 0;
  ACTIVE = 1;
  INACTIVE = 2;
}

// ✅ Message imbriqué
message Order {
  string id = 1;
  repeated OrderItem items = 2;  // Liste d'OrderItem
}

message OrderItem {
  string product_id = 1;
  int32 quantity = 2;
}
```

## 🔍 Exemple Complet : OrderService

### 1. Créer le Fichier .proto (MANUEL)

**`Protos/order.proto`**
```protobuf
syntax = "proto3";

option csharp_namespace = "KBA.Framework.Grpc.Orders";

service OrderService {
  rpc CreateOrder (CreateOrderRequest) returns (CreateOrderResponse);
  rpc GetOrder (GetOrderRequest) returns (GetOrderResponse);
}

message CreateOrderRequest {
  string user_id = 1;
  repeated OrderItem items = 2;
}

message OrderItem {
  string product_id = 1;
  int32 quantity = 2;
  double unit_price = 3;
}

message CreateOrderResponse {
  bool success = 1;
  Order order = 2;
}

message Order {
  string id = 1;
  string user_id = 2;
  repeated OrderItem items = 3;
  double total_amount = 4;
}

message GetOrderRequest {
  string order_id = 1;
}

message GetOrderResponse {
  bool success = 1;
  Order order = 2;
}
```

### 2. Compiler (AUTOMATIQUE)

```bash
dotnet build
```

### 3. Utiliser le Code Généré

**Serveur :**
```csharp
using KBA.Framework.Grpc.Orders;

public class OrderGrpcService : OrderService.OrderServiceBase
{
    // Le compilateur a généré OrderServiceBase automatiquement!
    public override async Task<CreateOrderResponse> CreateOrder(
        CreateOrderRequest request,
        ServerCallContext context)
    {
        // Votre logique ici
        return new CreateOrderResponse
        {
            Success = true,
            Order = new Order { Id = Guid.NewGuid().ToString() }
        };
    }
}
```

**Client :**
```csharp
using KBA.Framework.Grpc.Orders;

// Le compilateur a généré OrderServiceClient automatiquement!
var channel = GrpcChannel.ForAddress("http://localhost:5005");
var client = new OrderService.OrderServiceClient(channel);

var request = new CreateOrderRequest
{
    UserId = userId.ToString(),
    Items = { 
        new OrderItem { ProductId = "...", Quantity = 2, UnitPrice = 99.99 }
    }
};

var response = await client.CreateOrderAsync(request);
```

## ⚙️ Workflow de Développement

### Étapes Pratiques

1. **Définir le contrat** (MANUEL)
   ```bash
   # Créer le fichier .proto
   nano Protos/monservice.proto
   ```

2. **Compiler** (AUTOMATIQUE)
   ```bash
   dotnet build
   ```

3. **Vérifier le code généré** (OPTIONNEL)
   ```bash
   # Fichiers dans obj/Debug/net8.0/
   ls obj/Debug/net8.0/*.cs
   ```

4. **Implémenter le serveur** (MANUEL)
   ```csharp
   public class MyGrpcService : MonService.MonServiceBase
   {
       public override async Task<Response> MaMethode(Request req, ServerCallContext ctx)
       {
           // Votre code
       }
   }
   ```

5. **Utiliser le client** (MANUEL)
   ```csharp
   var client = new MonService.MonServiceClient(channel);
   var response = await client.MaMethodeAsync(request);
   ```

## 🚨 Erreurs Courantes

### Erreur 1 : "Service not found"

**Cause :** Fichier `.proto` non inclus dans le projet

**Solution :**
```xml
<ItemGroup>
  <Protobuf Include="Protos\**\*.proto" GrpcServices="Both" />
</ItemGroup>
```

### Erreur 2 : "Type not defined"

**Cause :** Vous avez modifié le `.proto` mais pas recompilé

**Solution :**
```bash
dotnet clean
dotnet build
```

### Erreur 3 : Numéros de champs en conflit

**Cause :**
```protobuf
message Test {
  string field1 = 1;
  string field2 = 1;  // ❌ Même numéro!
}
```

**Solution :**
```protobuf
message Test {
  string field1 = 1;
  string field2 = 2;  // ✅ Numéro unique
}
```

## 📚 Ressources

- **Documentation officielle** : https://protobuf.dev/
- **gRPC C#** : https://grpc.io/docs/languages/csharp/
- **Style Guide** : https://protobuf.dev/programming-guides/style/

## 💡 Bonnes Pratiques

### ✅ DO

```protobuf
// Noms explicites
message CreateUserRequest { }

// Champs optionnels quand approprié
optional string tenant_id = 3;

// Comments pour documenter
// Crée un nouvel utilisateur dans le système
rpc CreateUser (CreateUserRequest) returns (CreateUserResponse);
```

### ❌ DON'T

```protobuf
// Noms vagues
message Req { }

// Ne jamais réutiliser les numéros de champs!
message User {
  string name = 1;
  // string old_field = 2;  // ❌ Ne pas supprimer, marquer comme reserved
}

// Correct :
message User {
  reserved 2;  // ✅ Marquer comme réservé
  string name = 1;
  string email = 3;
}
```

## 🎉 Résumé

| Aspect | Réponse |
|--------|---------|
| **Qui crée les .proto ?** | Vous, manuellement |
| **Quand ?** | Avant d'implémenter le service gRPC |
| **Code C# généré ?** | Oui, automatiquement lors du build |
| **À quoi ça sert ?** | Définir le contrat entre services |
| **Avantages ?** | Typage fort, performance, compatibilité |

---

**Le fichier .proto = Le blueprint de votre API gRPC** 🏗️

Vous le concevez manuellement, les outils construisent automatiquement le reste !
