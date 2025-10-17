using System.Text;
using KBA.TenantService.Data;
using KBA.TenantService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

// Créer une configuration temporaire pour lire les settings
var tempConfig = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var seqUrl = tempConfig["Serilog:SeqUrl"] ?? "http://localhost:5341";

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/tenant-service-.log", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq(seqUrl)
    .Enrich.WithProperty("Service", "TenantService")
    .CreateLogger();

try
{
    Log.Information("Démarrage du Tenant Service on {SeqUrl}", seqUrl);

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // Database
    builder.Services.AddDbContext<TenantDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            b => b.MigrationsAssembly("KBA.TenantService")));

    // Services
    builder.Services.AddScoped<ITenantServiceLogic, TenantServiceLogic>();

    // JWT Authentication
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey manquante");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
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
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "KBA Tenant Service", Version = "v1" });
    });

    builder.Services.AddHealthChecks()
        .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string not found"));

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
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tenant Service v1"));
    }

    app.UseSerilogRequestLogging();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();
        db.Database.Migrate();
        Log.Information("Migrations appliquées avec succès");
    }

    Log.Information("Tenant Service démarré sur http://localhost:5003");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Tenant Service s'est arrêté de manière inattendue");
}
finally
{
    Log.CloseAndFlush();
}
