using System.ComponentModel.DataAnnotations;

namespace Demo.Api.Configuration;

/// <summary>
/// Strongly typed configuration for the Keycloak resource server integration.
/// </summary>
public sealed class KeycloakAuthenticationOptions
{
    public const string SectionName = "Authentication";

    [Required]
    public string Authority { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    public bool RequireHttpsMetadata { get; init; } = true;

    public bool ValidateIssuer { get; init; } = true;

    public string NameClaimType { get; init; } = "preferred_username";

    public string RoleClaimType { get; init; } = "roles";

    public void Validate()
    {
        if (!Uri.TryCreate(Authority, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("Authentication.Authority must be an absolute URI.");
        }

        if (string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("Authentication.Audience must be provided.");
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
