using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Proyecto.Data;
using Proyecto.Hubs;
using Proyecto.Models;
using Proyecto.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ── Docker Swarm Secrets ─────────────────────────────────────────────────────
// Los secrets se montan en /run/secrets/<nombre>. Si existe el archivo,
// su contenido sobreescribe la configuración equivalente.
static void LoadDockerSecret(WebApplicationBuilder b, string secretName, string configKey)
{
    var path = $"/run/secrets/{secretName}";
    if (File.Exists(path))
    {
        var value = File.ReadAllText(path).Trim();
        b.Configuration[configKey] = value;
    }
}

LoadDockerSecret(builder, "db_connection_string",   "ConnectionStrings:DefaultConnection");
LoadDockerSecret(builder, "paypal_client_id",        "PayPal:ClientId");
LoadDockerSecret(builder, "paypal_client_secret",    "PayPal:ClientSecret");
LoadDockerSecret(builder, "payphone_token",          "PayPhone:Token");
LoadDockerSecret(builder, "payphone_store_id",       "PayPhone:StoreId");
LoadDockerSecret(builder, "email_smtp_password",     "Email:Smtp:Password");
LoadDockerSecret(builder, "google_client_id",        "Authentication:Google:ClientId");
LoadDockerSecret(builder, "google_client_secret",    "Authentication:Google:ClientSecret");

// ── MVC ─────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── Estado distribuido opcional ─────────────────────────────────────────────
var redisConnection = builder.Configuration["Redis__ConnectionString"]
    ?? builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "EmprendeMarket:";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.Name = ".EmprendeMarket.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

var signalRBuilder = builder.Services.AddSignalR();
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    signalRBuilder.AddStackExchangeRedis(redisConnection);
}

// ── Base de datos ────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── ASP.NET Core Identity (con int como PK) ──────────────────────────────────
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
    {
        // Requisitos de contraseña
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;

        // Email único
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// ── Autenticación externa con Google (OAuth) ───────────────────────────────────
// Las credenciales se leen desde User Secrets / .env (nunca del código).
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            // Tras el login, Identity envía al callback que nosotros manejamos.
            options.CallbackPath = "/signin-google";
            // Permitir que la cookie de correlación funcione en HTTP local
            // (evita "Correlation failed" al volver de Google).
            options.CorrelationCookie.SameSite = SameSiteMode.Lax;
            options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.CorrelationCookie.HttpOnly = true;
        });
}

// ── Servicios propios ────────────────────────────────────────────────────────
builder.Services.AddScoped<IUserSessionService, UserSessionService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddHttpClient<ILocalAiService, OllamaService>(client =>
{
    var baseUrl = builder.Configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(90);
});

builder.Services.AddHttpClient<IPayPhoneService, PayPhoneService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddScoped<IPayPalService, PayPalService>();

// ── Nuevos servicios ─────────────────────────────────────────────────────────
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<PayPalPaymentGateway>();
builder.Services.AddScoped<PayPhonePaymentGateway>();
builder.Services.AddScoped<IMovimientoInventarioService, MovimientoInventarioService>();
builder.Services.AddScoped<INotificacionService, NotificacionService>();

// ── Data Protection compartido cuando Redis está disponible ──────────────────
var dataProtection = builder.Services.AddDataProtection()
    .SetApplicationName("EmprendeMarket");
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    dataProtection.PersistKeysToStackExchangeRedis(
        ConnectionMultiplexer.Connect(redisConnection),
        "EmprendeMarket-DataProtection-Keys");
}
else
{
    var keysPath = builder.Configuration["DataProtection:KeysPath"]
        ?? Path.Combine(builder.Environment.ContentRootPath, "dataprotection-keys");
    Directory.CreateDirectory(keysPath);
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection es obligatoria."),
        name: "postgresql");
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddHealthChecks().AddRedis(redisConnection, "redis");
}

var app = builder.Build();

// ── Forwarded Headers (detectar proxy/nginx) ──────────────────────────────────
var forwardedHeaders = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                       ForwardedHeaders.XForwardedProto |
                       ForwardedHeaders.XForwardedHost
};
forwardedHeaders.KnownNetworks.Clear();
forwardedHeaders.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeaders);
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Instance"] =
        Environment.GetEnvironmentVariable("POD_NAME") ?? Environment.MachineName;
    await next();
});

// ── Pipeline HTTP ─────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    // En desarrollo: redirigir a HTTPS y usar HSTS
    app.UseHttpsRedirection();
    app.UseHsts();
}
else
{
    // En producción dentro de Docker: sin HTTPS redirect ni HSTS.
    // El TLS lo maneja el reverse proxy externo (nginx/traefik).
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();
app.UseSession();

app.UseAuthentication();    // requerido por Identity (aunque la sesión siga activa)
app.UseAuthorization();

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready") || check.Tags.Count == 0
});
app.MapHub<NotificacionesHub>("/hubs/notificaciones");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();

// ── ✅ CAMBIO 1: Las migraciones se ejecutan AHORA, después de configurar el pipeline ──
// Esto asegura que la aplicación pueda iniciar incluso si hay errores de migración
var runMigrations = app.Environment.IsDevelopment()
    || builder.Configuration.GetValue<bool>("RunMigrations");
var migrationsOnly = builder.Configuration.GetValue<bool>("RunMigrationsOnly");

if (runMigrations)
{
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Aplicando {Count} migraciones pendientes...", pendingMigrations.Count());
                await context.Database.MigrateAsync();
            }

            if (!await context.Categorias.AnyAsync())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
                await AppDbContext.SeedData(context, userManager, roleManager);
                logger.LogInformation("Datos iniciales insertados correctamente.");
            }
        }
        if (migrationsOnly)
        {
            return;
        }
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al aplicar migraciones o seed.");
        if (migrationsOnly)
        {
            throw;
        }
    }
}

app.Run();
