namespace Demo.Web.Configuration;

/// <summary>
/// Describes outbound HTTP endpoints consumed by the web application.
/// </summary>
public sealed class ApiEndpointsOptions
{
    public const string SectionName = "Endpoints";
    public const string HttpClientName = "KeycloakApi";

    public string ApiBaseUrl { get; init; } = string.Empty;

    public Uri ApiBaseUri => new(ApiBaseUrl, UriKind.Absolute);

    public void Validate()
    {
        if (!Uri.TryCreate(ApiBaseUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException("Endpoints.ApiBaseUrl must be an absolute URI.");
        }
    }
}
