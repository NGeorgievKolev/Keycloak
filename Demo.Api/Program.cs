using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("https://localhost:5002", "http://localhost:5003");

var authority = "http://localhost:8080/realms/demo";
var audience = "dotnet-api"; // това е Client ID-то, което зададе като audience

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authority;
        options.Audience = audience;
        options.RequireHttpsMetadata = false; // защото Keycloak е на http локално
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "preferred_username",
            RoleClaimType = "roles"
        };
    });

builder.Services.AddAuthorization();

// Разрешаваме заявки от Web апа (CORS)
builder.Services.AddCors(p => p.AddDefaultPolicy(policy =>
    policy.WithOrigins("https://localhost:5001")
          .AllowAnyHeader()
          .AllowAnyMethod()));

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Публичен route
app.MapGet("/ping", () => "pong");

// Защитен route – изисква валиден access token
app.MapGet("/me", (HttpContext ctx) =>
{
    if (!ctx.User.Identity?.IsAuthenticated ?? true)
        return Results.Unauthorized();

    var name = ctx.User.Identity?.Name ?? ctx.User.FindFirst("preferred_username")?.Value ?? "unknown";
    var roles = ctx.User.FindAll("roles").Select(r => r.Value).ToArray();
    return Results.Ok(new { name, roles });
}).RequireAuthorization();

app.Run();
