using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using QuestPDF.Infrastructure;
using Restaurant_Menu.Hubs;
using Restaurant_Menu.Implementation;
using Restaurant_Menu.Interface;
using Restaurant_Menu.Repositories;
using Restaurant_Menu.Services;
using System.Text;
using System.Text.Json.Serialization;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

/*──────────────────────── 0. SERVICES ──────────────────────*/
// HttpClient factory for OpenAI
builder.Services.AddHttpClient();
builder.Services.AddScoped<IPrintForwarder, PrintForwarder>();

// Chatbot Service
builder.Services.AddScoped<ChatbotService>();

/*──────────────────────── 1. CORS ────────────────────────*/
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()
));


/*──────────────────────── 2. SignalR ─────────────────────*/
builder.Services.AddSignalR();

/*──────────────────────── 3. MVC / JSON ──────────────────*/
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

/*──────────────────────── 4. Swagger ─────────────────────*/
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

/*──────────────────────── 5. Database (Local + Render) ───*/
string conn;
var envUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrWhiteSpace(envUrl))
{
    if (envUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        envUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        // Render-style URL: postgres://user:pass@host:port/db
        var dbUri = new Uri(envUrl);
        var userInfo = (dbUri.UserInfo ?? "").Split(':', 2, StringSplitOptions.None);
        var username = userInfo.Length > 0 ? userInfo[0] : "";
        var password = userInfo.Length > 1 ? userInfo[1] : "";
        var port = dbUri.Port > 0 ? dbUri.Port : 5432;

        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = dbUri.Host,
            Port = port,
            Database = dbUri.AbsolutePath.TrimStart('/'),
            Username = username,
            Password = password,
            SslMode = SslMode.Require,
            TrustServerCertificate = true,
            Pooling = true
        };

        conn = csb.ConnectionString;
    }
    else
    {
        // Already a connection string (Host=...;User Id=...; etc.)
        conn = envUrl;
    }
}
else
{
    // Fallback to appsettings.json
    conn = builder.Configuration.GetConnectionString("DefaultConnection");
}

var safeConn = MaskPasswordFromConnectionString(conn);
Console.WriteLine($"Using DB connection: {safeConn}");

builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseNpgsql(conn));

/*──────────────────────── 6. DI (Repositories) ───────────*/
builder.Services.AddScoped<IOrderItemRepository, OrderItemRepository>();
builder.Services.AddScoped<IRestaurantTableRepository, RestaurantTableRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ISubCategoryRepository, SubCategoryRepository>();
builder.Services.AddScoped<IOfferRepository, OfferRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();

/*──────────────────────── 7. JWT ─────────────────────────*/
var jwtKey = builder.Configuration["Jwt:Key"] ?? "super-secret-key";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Restaurant-Menu";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(token) && path.StartsWithSegments("/hubs/order"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

/*──────────────────────── 8. Host URLs ──────────────────*/
var portEnv = Environment.GetEnvironmentVariable("PORT");
var urlsEnv = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");

// Default to listen on all interfaces so reverse proxy/external requests work
if (string.IsNullOrWhiteSpace(urlsEnv) && string.IsNullOrWhiteSpace(portEnv))
{
    builder.WebHost.UseUrls("http://0.0.0.0:5088", "https://0.0.0.0:5001");
}
else if (!string.IsNullOrWhiteSpace(portEnv))
{
    builder.WebHost.UseUrls($"http://*:{portEnv}");
}

/*──────────────────────── BUILD ──────────────────────────*/
var app = builder.Build();

/* Enable forwarded headers so app understands X-Forwarded-Proto / X-Forwarded-For */
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

/* Swagger - enabled in all environments so /swagger is available after publish */
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Restaurant-Menu API v1");
    c.RoutePrefix = "swagger";
});

/* Global JSON error response */
app.UseExceptionHandler("/error");
app.Map("/error", a => a.Run(async ctx =>
{
    var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
    await ctx.Response.WriteAsJsonAsync(new { error = ex?.Message });
}));

/* Fix Render deployment */
if (!app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        context.Request.Scheme = "https";
        await next();
    });
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

/*──────────── Endpoints ────────────*/
app.MapControllers();
app.MapHub<OrderHub>("/hubs/order");

/* Health check */
app.MapGet("/", () => "✅ ScanUI backend is running!");
app.MapFallbackToFile("index.html");

app.Run();

/* ----------------- Helper ----------------- */
static string MaskPasswordFromConnectionString(string cs)
{
    try
    {
        var builder = new NpgsqlConnectionStringBuilder(cs);
        if (!string.IsNullOrEmpty(builder.Password))
            builder.Password = "*****";
        return builder.ConnectionString;
    }
    catch
    {
        return System.Text.RegularExpressions.Regex.Replace(
            cs,
            "(Password|Pwd)=[^;]+",
            "$1=*****",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}
