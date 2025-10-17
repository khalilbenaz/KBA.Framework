using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;

// Configuration Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/api-gateway-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Démarrage de l'API Gateway");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // Ocelot configuration
    builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
    builder.Services.AddOcelot(builder.Configuration);

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });

    // Swagger pour la documentation
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() 
        { 
            Title = "KBA API Gateway", 
            Version = "v1",
            Description = "Point d'entrée unique pour tous les microservices KBA Framework"
        });
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway v1");
        });
    }

    app.UseSerilogRequestLogging();
    app.UseCors();

    // Page d'accueil
    app.MapGet("/", () => Results.Content(@"
<!DOCTYPE html>
<html>
<head>
    <title>KBA API Gateway</title>
    <style>
        body { font-family: Arial, sans-serif; max-width: 800px; margin: 50px auto; padding: 20px; }
        h1 { color: #333; }
        .service { background: #f4f4f4; padding: 15px; margin: 10px 0; border-radius: 5px; }
        .endpoint { color: #0066cc; }
        a { color: #0066cc; text-decoration: none; }
        a:hover { text-decoration: underline; }
    </style>
</head>
<body>
    <h1>🚀 KBA Framework - API Gateway</h1>
    <p>Point d'entrée unique pour l'architecture microservices</p>
    
    <h2>📡 Services Disponibles</h2>
    
    <div class='service'>
        <h3>🔐 Identity Service</h3>
        <p><strong>Base URL:</strong> <span class='endpoint'>http://localhost:5000/api/identity</span></p>
        <ul>
            <li>POST /api/identity/auth/login - Se connecter</li>
            <li>POST /api/identity/auth/register - S'enregistrer</li>
            <li>GET /api/identity/users - Liste des utilisateurs</li>
        </ul>
    </div>
    
    <div class='service'>
        <h3>📦 Product Service</h3>
        <p><strong>Base URL:</strong> <span class='endpoint'>http://localhost:5000/api/products</span></p>
        <ul>
            <li>GET /api/products - Liste des produits</li>
            <li>GET /api/products/{id} - Détails d'un produit</li>
            <li>POST /api/products - Créer un produit (nécessite auth)</li>
        </ul>
    </div>
    
    <div class='service'>
        <h3>🏢 Tenant Service</h3>
        <p><strong>Base URL:</strong> <span class='endpoint'>http://localhost:5000/api/tenants</span></p>
        <ul>
            <li>GET /api/tenants - Liste des tenants</li>
            <li>GET /api/tenants/{id} - Détails d'un tenant</li>
        </ul>
    </div>
    
    <h2>📚 Documentation</h2>
    <ul>
        <li><a href='/swagger'>Swagger UI</a> - Documentation interactive</li>
        <li><a href='/health'>Health Check</a> - État du gateway</li>
    </ul>
    
    <h2>🔗 Services Directs (Développement)</h2>
    <ul>
        <li><a href='http://localhost:5001/swagger' target='_blank'>Identity Service</a> (Port 5001)</li>
        <li><a href='http://localhost:5002/swagger' target='_blank'>Product Service</a> (Port 5002)</li>
        <li><a href='http://localhost:5003/swagger' target='_blank'>Tenant Service</a> (Port 5003)</li>
    </ul>
</body>
</html>
    ", "text/html"));

    // Health check
    app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "API Gateway" }));

    await app.UseOcelot();

    Log.Information("API Gateway démarré sur http://localhost:5000");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API Gateway s'est arrêté de manière inattendue");
}
finally
{
    Log.CloseAndFlush();
}
