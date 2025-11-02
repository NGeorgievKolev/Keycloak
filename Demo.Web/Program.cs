using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("https://localhost:5001", "http://localhost:5004");

builder.Services.AddRazorPages();

// Конфигурация за OpenID Connect (Keycloak)
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddOpenIdConnect(options =>
    {
        options.Authority = "http://localhost:8080/realms/demo";
        options.ClientId = "dotnet-web";
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.CallbackPath = "/signin-oidc";
        options.SignedOutCallbackPath = "/signout-callback-oidc";
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.RequireHttpsMetadata = false; // защото Keycloak е на http локално

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");

        options.TokenValidationParameters.NameClaimType = "preferred_username";
        options.TokenValidationParameters.RoleClaimType = "roles";
    });

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// 👇 Тестови route-ове за пилота
app.MapGet("/", () =>
    Results.Text("<a href=\"/secure\">Secure page</a> | <a href=\"/call-api\">Call API</a>", "text/html")
);

app.MapGet("/secure", (HttpContext ctx) =>
{
    if (!ctx.User.Identity?.IsAuthenticated ?? true)
        return Results.Challenge(); // ще прати към Keycloak login

    var name = ctx.User.Identity?.Name ?? "unknown";
    return Results.Text($"Hello, {name}! You are authenticated via Keycloak.", "text/plain");
});

// Тестов route за извикване на API-то със същия access_token
app.MapGet("/call-api", async (HttpContext ctx) =>
{
    if (!ctx.User.Identity?.IsAuthenticated ?? true)
        return Results.Challenge();

    var accessToken = await ctx.GetTokenAsync("access_token");
    using var http = new HttpClient();
    http.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

    var apiUrl = "https://localhost:5002/me"; // към API-то
    var resp = await http.GetAsync(apiUrl);
    var body = await resp.Content.ReadAsStringAsync();
    return Results.Text($"API Response ({resp.StatusCode}):\n{body}", "text/plain");
});

app.Run();
