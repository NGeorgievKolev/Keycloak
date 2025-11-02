using System.Security.Claims;
using Demo.Api.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

// Create the host builder and pin the demo API to fixed ports so the accompanying web
// application can call it without additional configuration.
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("https://localhost:5002", "http://localhost:5003");

// Load the Keycloak resource-server configuration (see appsettings.json) and fail fast if
// the configuration is incomplete. This keeps startup errors obvious for local development.
var authenticationOptions = builder.Configuration
    .GetRequiredSection(KeycloakAuthenticationOptions.SectionName)
    .Get<KeycloakAuthenticationOptions>()
    ?? throw new InvalidOperationException("Keycloak authentication configuration is missing.");

authenticationOptions.Validate();

// CORS is optional in configuration. If omitted we use the defaults (deny all) so that the
// validation below can surface a helpful error message when a web origin has to be added.
var corsSettings = builder.Configuration
    .GetSection(CorsSettings.SectionName)
    .Get<CorsSettings>()
    ?? new CorsSettings();

corsSettings.Validate();

builder.Services.AddSingleton(authenticationOptions);
builder.Services.AddSingleton(corsSettings);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure the API as a resource server. The JWT bearer handler will validate access tokens
// issued by the Keycloak realm using the metadata exposed by the authority URL.
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

// Only allow the origins that have been explicitly configured. This keeps the demo secure by
// default and mirrors what you would typically do in production.
builder.Services.AddCors(policy =>
    policy.AddDefaultPolicy(corsPolicyBuilder =>
        corsPolicyBuilder
            .WithOrigins(corsSettings.AllowedOrigins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/ping", () => Results.Ok("pong"));

// This endpoint shows the claims that Keycloak puts in the access token so that frontends have
// a quick way to confirm the resource-server setup works.
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
