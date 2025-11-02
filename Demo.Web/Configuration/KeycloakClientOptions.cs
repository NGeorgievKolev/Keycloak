namespace Demo.Web.Configuration;

/// <summary>
/// Strongly typed configuration for the interactive client relying on Keycloak.
/// </summary>
public sealed class KeycloakClientOptions
{
    public const string SectionName = "Authentication";

    public string Authority { get; init; } = string.Empty;

    public string ClientId { get; init; } = string.Empty;

    public string? ResponseType { get; init; }
        = Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectResponseType.Code;

    public string CallbackPath { get; init; } = "/signin-oidc";

    public string SignedOutCallbackPath { get; init; } = "/signout-callback-oidc";

    public bool RequireHttpsMetadata { get; init; } = true;

    public string NameClaimType { get; init; } = "preferred_username";

    public string RoleClaimType { get; init; } = "roles";

    public IReadOnlyCollection<string> Scopes { get; init; } = new[] { "openid" };

    public void Validate()
    {
        if (!Uri.TryCreate(Authority, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("Authentication.Authority must be an absolute URI.");
        }

        if (string.IsNullOrWhiteSpace(ClientId))
        {
            throw new InvalidOperationException("Authentication.ClientId must be provided.");
        }

        if (Scopes.Count == 0 || Scopes.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("Authentication.Scopes must contain at least one valid scope.");
        }

        if (string.IsNullOrWhiteSpace(NameClaimType))
        {
            throw new InvalidOperationException("Authentication.NameClaimType must be provided.");
        }

        if (string.IsNullOrWhiteSpace(RoleClaimType))
        {
            throw new InvalidOperationException("Authentication.RoleClaimType must be provided.");
        }
    }
}
