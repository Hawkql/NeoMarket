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
            JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.DefaultIgnoreCondition =
            JsonIgnoreCondition.WhenWritingNull;
    });

// ============ JWT Authentication ============
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret not configured");

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
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine($">>> JWT FAILED: {ctx.Exception.GetType().Name}: {ctx.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                var claims = string.Join(", ",
                    ctx.Principal!.Claims.Select(c => $"{c.Type}={c.Value}"));
                Console.WriteLine($">>> JWT OK. Claims: {claims}");
                return Task.CompletedTask;
            },
            OnChallenge = ctx =>
            {
                Console.WriteLine($">>> JWT CHALLENGE: {ctx.Error} / {ctx.ErrorDescription}");
                return Task.CompletedTask;
            },
            OnForbidden = ctx =>
            {
                Console.WriteLine($">>> JWT FORBIDDEN: токен валиден, но роль не подходит");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SellerOnly", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(c =>
                (c.Type == "role" || c.Type == System.Security.Claims.ClaimTypes.Role)
                && c.Value == "seller")));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();





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