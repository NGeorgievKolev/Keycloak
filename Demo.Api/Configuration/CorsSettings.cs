namespace Demo.Api.Configuration;

/// <summary>
/// Strongly typed configuration for Cross-Origin Resource Sharing (CORS).
/// </summary>
public sealed class CorsSettings
{
    public const string SectionName = "Cors";

    public IReadOnlyCollection<string> AllowedOrigins { get; init; } = Array.Empty<string>();

    public void Validate()
    {
        if (AllowedOrigins.Count == 0)
        {
            throw new InvalidOperationException("At least one CORS origin must be configured in Cors.AllowedOrigins.");
        }

        if (AllowedOrigins.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("CORS origins cannot be null or whitespace.");
        }
    }
}
