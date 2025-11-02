using System.Net.Http.Headers;
using System.Net.Mime;
using System.Security.Claims;
using Demo.Web.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

// Spin up the interactive client on fixed ports so it lines up with the Keycloak realm
// configuration documented in README.md and the launchSettings profiles.
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("https://localhost:5001", "http://localhost:5004");

// The Authentication section describes how the SPA-like demo should talk to Keycloak. Loading
// the settings up-front keeps misconfiguration errors obvious to whoever runs the demo next.
var keycloakClientOptions = builder.Configuration
    .GetRequiredSection(KeycloakClientOptions.SectionName)
    .Get<KeycloakClientOptions>()
    ?? throw new InvalidOperationException("Authentication configuration is missing.");

keycloakClientOptions.Validate();

// The Endpoints section lists downstream APIs we call. For now we only have the demo API, but
// this pattern makes it easy to add more service calls later.
var endpointsOptions = builder.Configuration
    .GetRequiredSection(ApiEndpointsOptions.SectionName)
    .Get<ApiEndpointsOptions>()
    ?? throw new InvalidOperationException("Endpoint configuration is missing.");

endpointsOptions.Validate();

builder.Services.AddSingleton(keycloakClientOptions);
builder.Services.AddSingleton(endpointsOptions);

builder.Services.AddRazorPages();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Wire up cookie + OpenID Connect authentication so ASP.NET Core can maintain the user session
// and automatically redirect unauthenticated users to Keycloak when needed.
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(options =>
    {
        options.Authority = keycloakClientOptions.Authority;
        options.ClientId = keycloakClientOptions.ClientId;
        options.ResponseType = keycloakClientOptions.ResponseType ?? OpenIdConnectResponseType.Code;
        options.CallbackPath = keycloakClientOptions.CallbackPath;
        options.SignedOutCallbackPath = keycloakClientOptions.SignedOutCallbackPath;
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.RequireHttpsMetadata = keycloakClientOptions.RequireHttpsMetadata;

        options.Scope.Clear();
        foreach (var scope in keycloakClientOptions.Scopes)
        {
            options.Scope.Add(scope);
        }

        options.TokenValidationParameters.NameClaimType = keycloakClientOptions.NameClaimType;
        options.TokenValidationParameters.RoleClaimType = keycloakClientOptions.RoleClaimType;
    });

// Register a named HttpClient so the secure endpoint can call the API using the base URL from
// configuration. Using dependency injection keeps the sample aligned with ASP.NET guidance.
builder.Services.AddHttpClient(ApiEndpointsOptions.HttpClientName, (sp, client) =>
{
    var endpoints = sp.GetRequiredService<ApiEndpointsOptions>();
    client.BaseAddress = endpoints.ApiBaseUri;
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.MapGet("/", () =>
    Results.Content("<a href=\"/secure\">Secure page</a> | <a href=\"/call-api\">Call API</a>", MediaTypeNames.Text.Html));

// A minimal secure page that demonstrates how to work with the authenticated user's claims.
app.MapGet("/secure", (ClaimsPrincipal user, KeycloakClientOptions options) =>
{
    if (user.Identity?.IsAuthenticated is not true)
    {
        return Results.Challenge();
    }

    var name = user.Identity?.Name ?? user.FindFirst(options.NameClaimType)?.Value ?? "unknown";
    return Results.Text($"Hello, {name}! You are authenticated via Keycloak.", MediaTypeNames.Text.Plain);
});

// Demonstrates how to forward the current user's access token to the protected API.
app.MapGet("/call-api", async (HttpContext context, IHttpClientFactory httpClientFactory, ApiEndpointsOptions options) =>
{
    if (context.User.Identity?.IsAuthenticated is not true)
    {
        return Results.Challenge();
    }

    var accessToken = await context.GetTokenAsync("access_token");
    if (string.IsNullOrWhiteSpace(accessToken))
    {
        return Results.Problem("Unable to resolve an access token for the current user.", statusCode: StatusCodes.Status401Unauthorized);
    }

    var client = httpClientFactory.CreateClient(ApiEndpointsOptions.HttpClientName);
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    var response = await client.GetAsync("me");
    var content = await response.Content.ReadAsStringAsync();
    var body = $"API Response ({(int)response.StatusCode} {response.StatusCode}):\n{content}";
    return Results.Text(body, MediaTypeNames.Text.Plain);
});

app.Run();
