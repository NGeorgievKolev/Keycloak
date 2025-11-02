using System.Security.Claims;
using Demo.Api.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("https://localhost:5002", "http://localhost:5003");

var authenticationOptions = builder.Configuration
    .GetRequiredSection(KeycloakAuthenticationOptions.SectionName)
    .Get<KeycloakAuthenticationOptions>()
    ?? throw new InvalidOperationException("Keycloak authentication configuration is missing.");

authenticationOptions.Validate();

var corsSettings = builder.Configuration
    .GetSection(CorsSettings.SectionName)
    .Get<CorsSettings>()
    ?? new CorsSettings();

corsSettings.Validate();

builder.Services.AddSingleton(authenticationOptions);
builder.Services.AddSingleton(corsSettings);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authenticationOptions.Authority;
        options.Audience = authenticationOptions.Audience;
        options.RequireHttpsMetadata = authenticationOptions.RequireHttpsMetadata;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = authenticationOptions.ValidateIssuer,
            NameClaimType = authenticationOptions.NameClaimType,
            RoleClaimType = authenticationOptions.RoleClaimType
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(policy =>
    policy.AddDefaultPolicy(corsPolicyBuilder =>
        corsPolicyBuilder
            .WithOrigins(corsSettings.AllowedOrigins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()));

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/ping", () => Results.Ok("pong"));

app.MapGet("/me", (ClaimsPrincipal user, KeycloakAuthenticationOptions options) =>
{
    if (user.Identity?.IsAuthenticated is not true)
    {
        return Results.Unauthorized();
    }

    var name = user.Identity?.Name
               ?? user.FindFirst(options.NameClaimType)?.Value
               ?? "unknown";
    var roles = user.FindAll(options.RoleClaimType).Select(r => r.Value).ToArray();
    return Results.Ok(new { name, roles });
}).RequireAuthorization();

app.Run();
