# Guide : Ajouter un Nouveau Microservice

## 📋 Exemple Pratique : Order Service

Nous allons créer un nouveau microservice de **gestion des commandes**.

### Étape 1 : Créer la structure du projet

```powershell
cd microservices
mkdir KBA.OrderService
cd KBA.OrderService
```

### Étape 2 : Créer le fichier .csproj

**KBA.OrderService.csproj** :

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <!-- ASP.NET Core -->
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
    
    <!-- Entity Framework Core -->
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    
    <!-- API Documentation -->
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
    
    <!-- Logging -->
    <PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
    <PackageReference Include="Serilog.Sinks.Console" Version="5.0.1" />
    <PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
    <PackageReference Include="Serilog.Sinks.Seq" Version="7.0.0" />
    
    <!-- Optional: Message Bus -->
    <PackageReference Include="MassTransit" Version="8.1.3" />
    <PackageReference Include="MassTransit.RabbitMQ" Version="8.1.3" />
  </ItemGroup>

  <ItemGroup>
    <!-- Référence au Domain partagé -->
    <ProjectReference Include="..\..\src\KBA.Framework.Domain\KBA.Framework.Domain.csproj" />
  </ItemGroup>

</Project>
```

### Étape 3 : Créer l'entité Order dans Domain

**src/KBA.Framework.Domain/Entities/Orders/Order.cs** :

```csharp
using KBA.Framework.Domain.Common;

namespace KBA.Framework.Domain.Entities.Orders;

public class Order : AggregateRoot<Guid>
{
    public Guid? TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime OrderDate { get; private set; }
    
    // Collection d'items
    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { } // Pour EF Core

    public Order(Guid? tenantId, Guid userId, string orderNumber, Guid? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new ArgumentException("Le numéro de commande ne peut pas être vide.");

        Id = Guid.NewGuid();
        TenantId = tenantId;
        UserId = userId;
        OrderNumber = orderNumber;
        Status = OrderStatus.Pending;
        OrderDate = DateTime.UtcNow;
        TotalAmount = 0;
        SetCreationInfo(createdBy);
    }

    public void AddItem(Guid productId, string productName, decimal unitPrice, int quantity)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Impossible d'ajouter des items à une commande non en attente.");

        var item = new OrderItem(Id, productId, productName, unitPrice, quantity);
        _items.Add(item);
        
        RecalculateTotal();
    }

    public void RemoveItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
        {
            _items.Remove(item);
            RecalculateTotal();
        }
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Seule une commande en attente peut être confirmée.");

        if (!_items.Any())
            throw new InvalidOperationException("Une commande doit contenir au moins un item.");

        Status = OrderStatus.Confirmed;
    }

    public void Ship()
    {
        if (Status != OrderStatus.Confirmed)
            throw new InvalidOperationException("Seule une commande confirmée peut être expédiée.");

        Status = OrderStatus.Shipped;
    }

    public void Complete()
    {
        if (Status != OrderStatus.Shipped)
            throw new InvalidOperationException("Seule une commande expédiée peut être complétée.");

        Status = OrderStatus.Completed;
    }

    public void Cancel()
    {
        if (Status is OrderStatus.Completed or OrderStatus.Cancelled)
            throw new InvalidOperationException("Une commande terminée ou annulée ne peut pas être annulée.");

        Status = OrderStatus.Cancelled;
    }

    private void RecalculateTotal()
    {
        TotalAmount = _items.Sum(i => i.Subtotal);
    }
}

public class OrderItem : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal Subtotal => UnitPrice * Quantity;

    private OrderItem() { }

    public OrderItem(Guid orderId, Guid productId, string productName, decimal unitPrice, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("La quantité doit être supérieure à 0.");

        Id = Guid.NewGuid();
        OrderId = orderId;
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new ArgumentException("La quantité doit être supérieure à 0.");

        Quantity = newQuantity;
    }
}

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Shipped = 2,
    Completed = 3,
    Cancelled = 4
}
```

### Étape 4 : Créer le DbContext

**Data/OrderDbContext.cs** :

```csharp
using KBA.Framework.Domain;
using KBA.Framework.Domain.Entities.Orders;
using Microsoft.EntityFrameworkCore;

namespace KBA.OrderService.Data;

public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Order Configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable(KBAConsts.TablePrefix + "Orders");
            entity.HasKey(o => o.Id);
            
            entity.Property(o => o.OrderNumber).IsRequired().HasMaxLength(50);
            entity.Property(o => o.TotalAmount).HasPrecision(18, 2);
            entity.Property(o => o.Status).HasConversion<int>();
            
            entity.HasIndex(o => o.OrderNumber).IsUnique();
            entity.HasIndex(o => o.TenantId);
            entity.HasIndex(o => o.UserId);
            entity.HasIndex(o => o.OrderDate);
            
            // Relation avec les items
            entity.HasMany(o => o.Items)
                  .WithOne()
                  .HasForeignKey(oi => oi.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
            
            entity.HasQueryFilter(o => !o.IsDeleted);
        });

        // OrderItem Configuration
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable(KBAConsts.TablePrefix + "OrderItems");
            entity.HasKey(oi => oi.Id);
            
            entity.Property(oi => oi.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(oi => oi.UnitPrice).HasPrecision(18, 2);
            
            entity.HasIndex(oi => oi.OrderId);
            entity.HasIndex(oi => oi.ProductId);
        });
    }
}
```

### Étape 5 : Créer les DTOs

**DTOs/OrderDTOs.cs** :

```csharp
namespace KBA.OrderService.DTOs;

public record OrderDto
{
    public Guid Id { get; init; }
    public Guid? TenantId { get; init; }
    public Guid UserId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime OrderDate { get; init; }
    public List<OrderItemDto> Items { get; init; } = new();
}

public record OrderItemDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal Subtotal { get; init; }
}

public record CreateOrderDto
{
    public Guid UserId { get; init; }
    public List<CreateOrderItemDto> Items { get; init; } = new();
}

public record CreateOrderItemDto
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
}

public record UpdateOrderStatusDto
{
    public string Action { get; init; } = string.Empty; // "confirm", "ship", "complete", "cancel"
}
```

### Étape 6 : Créer le Service

**Services/OrderServiceLogic.cs** :

```csharp
using KBA.Framework.Domain.Entities.Orders;
using KBA.OrderService.Data;
using KBA.OrderService.DTOs;
using Microsoft.EntityFrameworkCore;

namespace KBA.OrderService.Services;

public interface IOrderServiceLogic
{
    Task<List<OrderDto>> GetAllAsync(Guid? userId = null);
    Task<OrderDto?> GetByIdAsync(Guid id);
    Task<OrderDto> CreateAsync(CreateOrderDto dto);
    Task<OrderDto> UpdateStatusAsync(Guid id, UpdateOrderStatusDto dto);
    Task DeleteAsync(Guid id);
}

public class OrderServiceLogic : IOrderServiceLogic
{
    private readonly OrderDbContext _context;
    private readonly ILogger<OrderServiceLogic> _logger;

    public OrderServiceLogic(OrderDbContext context, ILogger<OrderServiceLogic> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<OrderDto>> GetAllAsync(Guid? userId = null)
    {
        var query = _context.Orders.Include(o => o.Items).AsQueryable();
        
        if (userId.HasValue)
            query = query.Where(o => o.UserId == userId.Value);
        
        var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
        return orders.Select(MapToDto).ToList();
    }

    public async Task<OrderDto?> GetByIdAsync(Guid id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);
        
        return order == null ? null : MapToDto(order);
    }

    public async Task<OrderDto> CreateAsync(CreateOrderDto dto)
    {
        var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        var order = new Order(null, dto.UserId, orderNumber);

        foreach (var item in dto.Items)
        {
            order.AddItem(item.ProductId, item.ProductName, item.UnitPrice, item.Quantity);
        }

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Order created: {OrderId} - {OrderNumber}", order.Id, order.OrderNumber);

        return MapToDto(order);
    }

    public async Task<OrderDto> UpdateStatusAsync(Guid id, UpdateOrderStatusDto dto)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            throw new KeyNotFoundException($"Order {id} not found");

        switch (dto.Action.ToLower())
        {
            case "confirm":
                order.Confirm();
                break;
            case "ship":
                order.Ship();
                break;
            case "complete":
                order.Complete();
                break;
            case "cancel":
                order.Cancel();
                break;
            default:
                throw new ArgumentException($"Invalid action: {dto.Action}");
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Order {OrderId} status updated to {Status}", order.Id, order.Status);

        return MapToDto(order);
    }

    public async Task DeleteAsync(Guid id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
            throw new KeyNotFoundException($"Order {id} not found");

        order.Delete();
        await _context.SaveChangesAsync();
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            TenantId = order.TenantId,
            UserId = order.UserId,
            OrderNumber = order.OrderNumber,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString(),
            OrderDate = order.OrderDate,
            Items = order.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                Subtotal = i.Subtotal
            }).ToList()
        };
    }
}
```

### Étape 7 : Créer le Controller

**Controllers/OrdersController.cs** :

```csharp
using KBA.OrderService.DTOs;
using KBA.OrderService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KBA.OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderServiceLogic _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderServiceLogic orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderDto>>> GetAll([FromQuery] Guid? userId = null)
    {
        var orders = await _orderService.GetAllAsync(userId);
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid id)
    {
        var order = await _orderService.GetByIdAsync(id);
        if (order == null)
            return NotFound();

        return Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderDto dto)
    {
        try
        {
            var order = await _orderService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<OrderDto>> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusDto dto)
    {
        try
        {
            var order = await _orderService.UpdateStatusAsync(id, dto);
            return Ok(order);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            await _orderService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
```

### Étape 8 : Créer Program.cs

**Program.cs** :

```csharp
using System.Text;
using KBA.OrderService.Data;
using KBA.OrderService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/order-service-.log", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq("http://localhost:5341")
    .Enrich.WithProperty("Service", "OrderService")
    .CreateLogger();

try
{
    Log.Information("Starting Order Service");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // Database - BASE UNIQUE
    builder.Services.AddDbContext<OrderDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            b => b.MigrationsAssembly("KBA.OrderService")));

    // Services
    builder.Services.AddScoped<IOrderServiceLogic, OrderServiceLogic>();

    // JWT
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey missing");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization();
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddHealthChecks().AddDbContextCheck<OrderDbContext>();

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        });
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    // Auto-apply migrations
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        db.Database.Migrate();
        Log.Information("Migrations applied");
    }

    Log.Information("Order Service started on http://localhost:5004");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Order Service failed to start");
}
finally
{
    Log.CloseAndFlush();
}
```

### Étape 9 : Créer appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Urls": "http://localhost:5004",
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=KBAFrameworkDb;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true"
  },
  "JwtSettings": {
    "SecretKey": "VotreCleSecrete_MinimumDe32Caracteres_PourSecuriteOptimale",
    "Issuer": "KBAFramework",
    "Audience": "KBAFrameworkUsers",
    "ExpirationInMinutes": 60
  }
}
```

### Étape 10 : Ajouter au Gateway

**KBA.ApiGateway/ocelot.json** - Ajouter :

```json
{
  "DownstreamPathTemplate": "/api/{everything}",
  "DownstreamScheme": "http",
  "DownstreamHostAndPorts": [
    {
      "Host": "localhost",
      "Port": 5004
    }
  ],
  "UpstreamPathTemplate": "/api/orders/{everything}",
  "UpstreamHttpMethod": [ "GET", "POST", "PUT", "DELETE", "PATCH" ],
  "Key": "order-service"
}
```

### Étape 11 : Ajouter à la solution

```powershell
cd ..
dotnet sln KBA.Microservices.sln add KBA.OrderService/KBA.OrderService.csproj
```

### Étape 12 : Créer la migration

```powershell
cd KBA.OrderService
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Étape 13 : Tester

```powershell
dotnet run
```

**Tester avec curl** :

```bash
# 1. Se connecter
curl -X POST http://localhost:5000/api/identity/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"Admin@123456"}'

# 2. Créer une commande
curl -X POST http://localhost:5000/api/orders \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "USER_ID",
    "items": [
      {
        "productId": "PRODUCT_ID",
        "productName": "iPhone 15",
        "unitPrice": 999.99,
        "quantity": 2
      }
    ]
  }'

# 3. Obtenir toutes les commandes
curl -X GET http://localhost:5000/api/orders \
  -H "Authorization: Bearer TOKEN"

# 4. Confirmer une commande
curl -X PATCH http://localhost:5000/api/orders/ORDER_ID/status \
  -H "Authorization: Bearer TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"action":"confirm"}'
```

## ✅ Checklist d'ajout d'un nouveau service

- [ ] Créer le projet (.csproj)
- [ ] Ajouter les entités dans Domain
- [ ] Créer le DbContext
- [ ] Créer les DTOs
- [ ] Créer le Service Logic
- [ ] Créer les Controllers
- [ ] Créer Program.cs
- [ ] Créer appsettings.json
- [ ] Ajouter la route dans ocelot.json
- [ ] Ajouter à la solution .sln
- [ ] Créer et appliquer les migrations
- [ ] Mettre à jour start-microservices.ps1
- [ ] Tester le service
- [ ] Mettre à jour la documentation

## 🎯 Résumé

Vous venez de créer un nouveau microservice complet ! Ce service :

✅ Partage le Domain avec les autres services  
✅ Utilise la même base de données  
✅ S'authentifie avec JWT  
✅ Est accessible via l'API Gateway  
✅ Logs avec Serilog  
✅ A des health checks  
✅ Expose une API Swagger

**Reproduisez ce pattern pour tous vos futurs microservices !**
