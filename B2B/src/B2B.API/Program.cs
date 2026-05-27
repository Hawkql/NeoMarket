using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using B2B.Api.Middleware;
using B2B.Api.Services;
using B2B.Application;
using B2B.Application.Common.Abstractions;
using B2B.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
// ============ —лои ============
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ============ HTTP context + CurrentUserService ============
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// ============ Controllers + JSON snake_case ============
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.SnakeCaseLower;

        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter(
                B2B.Api.Serialization.UpperSnakeCaseNamingPolicy.Instance));
    });

// ============ JWT Authentication ============
var jwtSecret = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("Jwt:SigningKey not configured");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],

            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret)),


            RoleClaimType = "role",
            NameClaimType = "sub"
        };

    })
    .AddScheme<B2B.Api.Authentication.ServiceKeyAuthenticationOptions,
          B2B.Api.Authentication.ServiceKeyAuthenticationHandler>(
    B2B.Api.Authentication.ServiceKeyAuthenticationOptions.SchemeName,
    options =>
    {
        options.ExpectedKey =
            builder.Configuration["ServiceKey:Incoming"] ?? string.Empty;
    });


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SellerOnly", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c =>
                (c.Type == "role" || c.Type == System.Security.Claims.ClaimTypes.Role)
                && c.Value == "seller")));
    options.AddPolicy("ServiceOnly", policy =>
    {
        policy.AddAuthenticationSchemes(
            B2B.Api.Authentication.ServiceKeyAuthenticationOptions.SchemeName);
        policy.RequireAuthenticatedUser();   // только факт успешной аутентификации по ServiceKey
    });
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(ctx =>
            ctx.User.HasClaim(c =>
                (c.Type == "role" || c.Type == System.Security.Claims.ClaimTypes.Role)
                && c.Value == "admin"));
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // ќписываем Bearer JWT Ч чтобы по€вилась кнопка Authorize
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "¬ведите JWT-токен (без слова Bearer Ч Swagger добавит сам)"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});





var app = builder.Build();

// ============ Pipeline ============
app.UseMiddleware<ExceptionHandlingMiddleware>();  // первым Ч ловит всЄ ниже

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// ƒл€ интеграционных тестов (WebApplicationFactory<Program>)
public partial class Program { }