using System.Net.Http.Headers;
using System.Net.Mime;
using System.Security.Claims;
using Demo.Web.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("https://localhost:5001", "http://localhost:5004");

var keycloakClientOptions = builder.Configuration
    .GetRequiredSection(KeycloakClientOptions.SectionName)
    .Get<KeycloakClientOptions>()
    ?? throw new InvalidOperationException("Authentication configuration is missing.");

keycloakClientOptions.Validate();

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

app.MapGet("/secure", (ClaimsPrincipal user, KeycloakClientOptions options) =>
{
    if (user.Identity?.IsAuthenticated is not true)
    {
        return Results.Challenge();
    }

    var name = user.Identity?.Name ?? user.FindFirst(options.NameClaimType)?.Value ?? "unknown";
    return Results.Text($"Hello, {name}! You are authenticated via Keycloak.", MediaTypeNames.Text.Plain);
});

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
