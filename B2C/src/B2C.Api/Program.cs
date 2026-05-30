using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using B2C.Api.Auth;
using B2C.Api.Middleware;
using B2C.Application;
using B2C.Application.Common.Abstractions;
using B2C.Infrastructure;
using B2C.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Ќе маппить JWT claims в длинные .NET-типы Ч оставл€ем 'sub' как 'sub'.
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// ============ Application + Infrastructure ============
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ============ HTTP context + порты, реализуемые в API ============
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ISessionContext, SessionContext>();

// ============ Controllers + JSON snake_case ============
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // ѕол€ DTO в snake_case: first_name, total_amount, и т.д.
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;

        // Enum-ы как строки в snake_case: "price_asc", "delivered", "in_stock".
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));

        options.JsonSerializerOptions.DefaultIgnoreCondition =
            JsonIgnoreCondition.WhenWritingNull;
    });

// ============ JWT Authentication (свой секрет B2C, per-service auth) ============
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer()  // настраиваем ниже через AddOptions Ч lazy
    .AddScheme<ServiceKeyAuthenticationOptions, ServiceKeyAuthenticationHandler>(
        ServiceKeyAuthenticationOptions.SchemeName,
        _ => { });  // ExpectedKey тоже задаЄм lazy ниже

// JWT options Ч читаем JwtSettings в момент resolve, когда Configuration уже финальный.
builder.Services
    .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, configuration) =>
    {
        var settings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("Jwt section not configured");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Issuer,

            ValidateAudience = true,
            ValidAudience = settings.Audience,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(settings.Secret)),

            NameClaimType = "sub",
            RoleClaimType = "role",
        };
    });

// ServiceKey options Ч та же проблема с eager binding, чиним тем же паттерном.
builder.Services
    .AddOptions<ServiceKeyAuthenticationOptions>(ServiceKeyAuthenticationOptions.SchemeName)
    .Configure<IConfiguration>((options, configuration) =>
    {
        options.ExpectedKey = configuration["ServiceKey:Incoming"] ?? string.Empty;
    });

builder.Services.AddAuthorization(options =>
{
    // “олько покупатель (валидный JWT).
    options.AddPolicy("BuyerOnly", policy =>
    {
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
    });

    // “олько межсервисный вызов (X-Service-Key) Ч дл€ приЄма от B2B и админ-операций.
    options.AddPolicy("ServiceOnly", policy =>
    {
        policy.AddAuthenticationSchemes(ServiceKeyAuthenticationOptions.SchemeName);
        policy.RequireAuthenticatedUser();
    });
});

// ============ Swagger ============
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "NeoMarket B2C API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT-токен покупател€ (вводить без префикса Bearer)",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ============ јвто-миграции при старте (опционально, удобно дл€ dev/docker) ============
if (app.Configuration.GetValue<bool>("ApplyMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider
        .GetRequiredService<B2C.Infrastructure.Persistence.B2CDbContext>();
    await db.Database.MigrateAsync();
}

// ============ Pipeline ============
app.UseMiddleware<ExceptionHandlingMiddleware>();   // самый внешний Ч ловит всЄ

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// ƒл€ интеграционных тестов через WebApplicationFactory<Program>.
public partial class Program { }