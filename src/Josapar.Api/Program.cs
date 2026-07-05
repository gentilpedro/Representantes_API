using Josapar.Api.Features.Agenda;
using Josapar.Api.Features.Auth;
using Josapar.Api.Features.Catalog;
using Josapar.Api.Features.Clients;
using Josapar.Api.Features.Dashboard;
using Josapar.Api.Features.Leads;
using Josapar.Api.Features.Notifications;
using Josapar.Api.Features.Orders;
using Josapar.Api.Features.Profile;
using Josapar.Api.Features.Reports;
using Josapar.Api.Features.Sync;
using Josapar.Api.Infrastructure.Auth;
using Josapar.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Postgres");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:Postgres não configurada.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddSingleton<JwtTokenGenerator>();

var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSigningKey = jwtSection["SigningKey"];
if (string.IsNullOrWhiteSpace(jwtSigningKey))
{
    throw new InvalidOperationException("Jwt:SigningKey não configurada.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Representantes API",
        Version = "v1",
        Description = "API que serve o app Flutter josapar_representantes — autenticação, catálogo, clientes, pedidos e sincronização.",
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Informe apenas o token JWT retornado pelo /api/auth/login (sem o prefixo 'Bearer').",
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", document), [] },
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

const string WebAppCorsPolicy = "WebApp";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy(WebAppCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/swagger"))
    {
        await next();
        return;
    }

    var swaggerUsername = builder.Configuration["Swagger:Username"];
    var swaggerPassword = builder.Configuration["Swagger:Password"];
    if (string.IsNullOrEmpty(swaggerUsername) || string.IsNullOrEmpty(swaggerPassword))
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        return;
    }

    if (TryGetBasicAuthCredentials(context.Request, out var user, out var password)
        && FixedTimeEquals(user, swaggerUsername)
        && FixedTimeEquals(password, swaggerPassword))
    {
        await next();
        return;
    }

    context.Response.Headers.WWWAuthenticate = "Basic realm=\"Swagger\"";
    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
});

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Representantes API v1");
    options.RoutePrefix = "swagger";
});

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbInitializer.SeedAsync(db);
}

app.UseCors(WebAppCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("/api/auth").WithTags("Autenticação").MapAuthEndpoints();
app.MapGroup("/api/products").WithTags("Catálogo").MapCatalogEndpoints().RequireAuthorization();
app.MapGroup("/api/clients").WithTags("Clientes").MapClientEndpoints().RequireAuthorization();
app.MapGroup("/api/orders").WithTags("Pedidos").MapOrderEndpoints().RequireAuthorization();
app.MapGroup("/api/leads").WithTags("Leads").MapLeadEndpoints().RequireAuthorization();
app.MapGroup("/api/agenda").WithTags("Agenda").MapAgendaEndpoints().RequireAuthorization();
app.MapGroup("/api/visits").WithTags("Visitas").MapVisitEndpoints().RequireAuthorization();
app.MapGroup("/api/dashboard").WithTags("Dashboard").MapDashboardEndpoints().RequireAuthorization();
app.MapGroup("/api/reports").WithTags("Relatórios").MapReportsEndpoints().RequireAuthorization();
app.MapGroup("/api/notifications").WithTags("Notificações").MapNotificationEndpoints().RequireAuthorization();
app.MapGroup("/api/profile").WithTags("Perfil").MapProfileEndpoints().RequireAuthorization();
app.MapGroup("/api/sync").WithTags("Sincronização").MapSyncEndpoints().RequireAuthorization();

app.Run();

static bool TryGetBasicAuthCredentials(HttpRequest request, out string user, out string password)
{
    user = "";
    password = "";

    var header = request.Headers.Authorization.ToString();
    if (!header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    try
    {
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header["Basic ".Length..].Trim()));
        var separatorIndex = decoded.IndexOf(':');
        if (separatorIndex < 0)
        {
            return false;
        }

        user = decoded[..separatorIndex];
        password = decoded[(separatorIndex + 1)..];
        return true;
    }
    catch (FormatException)
    {
        return false;
    }
}

static bool FixedTimeEquals(string left, string right)
{
    var leftBytes = Encoding.UTF8.GetBytes(left);
    var rightBytes = Encoding.UTF8.GetBytes(right);
    return leftBytes.Length == rightBytes.Length && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
}
