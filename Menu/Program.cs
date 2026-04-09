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
builder.Services.AddHttpClient();
builder.Services.AddHostedService<SelfPingService>(); //  ADD THIS LINE
builder.Services.AddScoped<IPrintForwarder, PrintForwarder>();
builder.Services.AddScoped<ChatbotService>();

/*──────────────────────── 1. CORS (FIXED) ──────────────────*/
builder.Services.AddCors(options =>
{
    options.AddPolicy("ScanUI", policy =>
    {
        policy
           .WithOrigins(
    "http://localhost:4200",
    "https://app.scanui.site"
)

            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

/*──────────────────────── 2. SignalR ─────────────────────*/
builder.Services.AddSignalR();

/*──────────────────────── 3. Controllers / JSON ──────────*/
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

/*──────────────────────── 4. Swagger ─────────────────────*/
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

/*──────────────────────── 5. Database (Render + Local) ───*/
string conn;
var envUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrWhiteSpace(envUrl))
{
    if (envUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        envUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        var dbUri = new Uri(envUrl);
        var userInfo = (dbUri.UserInfo ?? "").Split(':', 2);

        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = dbUri.Host,
            Port = dbUri.Port > 0 ? dbUri.Port : 5432,
            Database = dbUri.AbsolutePath.TrimStart('/'),
            Username = userInfo[0],
            Password = userInfo.Length > 1 ? userInfo[1] : "",
            SslMode = SslMode.Require,
            TrustServerCertificate = true,
            Pooling = true
        };

        conn = csb.ConnectionString;
    }
    else
    {
        conn = envUrl;
    }
}
else
{
    conn = builder.Configuration.GetConnectionString("DefaultConnection");
}

builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseNpgsql(conn));

/*──────────────────────── 6. DI Repositories ─────────────*/
builder.Services.AddScoped<IOrderItemRepository, OrderItemRepository>();
builder.Services.AddScoped<IRestaurantTableRepository, RestaurantTableRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ISubCategoryRepository, SubCategoryRepository>();
builder.Services.AddScoped<IOfferRepository, OfferRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();

/*──────────────────────── 7. JWT Auth ────────────────────*/
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
                if (!string.IsNullOrEmpty(token) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/hubs/order"))
                {
                    ctx.Token = token;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

/*──────────────────────── BUILD ──────────────────────────*/
var app = builder.Build();

/* Forwarded headers (Render / Proxy support) */
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto
});

/* Swagger (enabled in prod) */
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Restaurant-Menu API v1");
    c.RoutePrefix = "swagger";
});

/* Global exception handler */
app.UseExceptionHandler("/error");
app.Map("/error", appErr =>
{
    appErr.Run(async ctx =>
    {
        var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
        await ctx.Response.WriteAsJsonAsync(new
        {
            error = ex?.Message ?? "Unexpected error"
        });
    });
});

/* Render HTTPS fix */
if (!app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        context.Request.Scheme = "https";
        await next();
    });
}

/* ─────────── MIDDLEWARE ORDER (IMPORTANT) ─────────── */

app.UseRouting();

app.UseCors("ScanUI");   // ✅ Correct placement

app.UseAuthentication();
app.UseAuthorization();

/* ─────────── ENDPOINTS ─────────── */

app.MapControllers();
app.MapHub<OrderHub>("/hubs/order");

/* Health check */
app.MapGet("/", () => "✅ ScanUI backend is running!");
app.MapGet("/api/health", () => "OK");

app.Run();
