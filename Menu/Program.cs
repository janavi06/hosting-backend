// Program.cs  –  run cleanly on *any* local machine
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using Restaurant_Menu.Hubs;
using Restaurant_Menu.Implementation;
using Restaurant_Menu.Interface;
using Restaurant_Menu.Repositories;
using System.Text;
using System.Text.Json.Serialization;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

/*──────────────────────── 1. CORS ────────────────────────*/
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(
        "http://localhost:4200",
        "https://scanui.netlify.app",
        "https://menu-view.netlify.app")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

/*──────────────────────── 2. SignalR ─────────────────────*/
builder.Services.AddSignalR();

/*──────────────────────── 3. MVC / JSON ──────────────────*/
builder.Services.AddControllers()
                .AddJsonOptions(o => o.JsonSerializerOptions.ReferenceHandler =
                                     ReferenceHandler.IgnoreCycles);

/*──────────────────────── 4. Swagger ─────────────────────*/
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

/*──────────────────────── 5. Database ────────────────────*/
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(opt => opt.UseNpgsql(conn));

/*──────────────────────── 6. DI (Repositories) ───────────*/
builder.Services.AddScoped<IOrderItemRepository, OrderItemRepository>();
builder.Services.AddScoped<IRestaurantTableRepository, RestaurantTableRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ISubCategoryRepository, SubCategoryRepository>();
builder.Services.AddScoped<IOfferRepository, OfferRepository>();

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

        /* SignalR query‑string token */
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

/*──────────────────────── 8. Host URLs (local‑friendly) ─*/
var portEnv = Environment.GetEnvironmentVariable("PORT");          // e.g. Render
var urlsEnv = Environment.GetEnvironmentVariable("ASPNETCORE_URLS"); // VS / dotnet run

if (string.IsNullOrWhiteSpace(urlsEnv) && string.IsNullOrWhiteSpace(portEnv))
{
    // fallback for plain `dotnet run`
    builder.WebHost.UseUrls("http://localhost:5088", "https://localhost:5001");
}
else if (!string.IsNullOrWhiteSpace(portEnv))
{
    builder.WebHost.UseUrls($"http://*:{portEnv}");
}

/*──────────────────────── BUILD ──────────────────────────*/
var app = builder.Build();

/* Swagger only in dev */
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Restaurant‑Menu API v1"));
}

/* Global JSON error response */
app.UseExceptionHandler("/error");
app.Map("/error", a => a.Run(async ctx =>
{
    var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
    await ctx.Response.WriteAsJsonAsync(new { error = ex?.Message });
}));

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

/*──────────── Endpoints ────────────*/
app.MapControllers();
app.MapHub<OrderHub>("/hubs/order");
app.MapFallbackToFile("index.html");

app.Run();
