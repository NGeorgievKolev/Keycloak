using Demo.Api.Configuration;
using Xunit;

namespace Demo.Api.Tests.Configuration;

public class CorsSettingsTests
{
    [Fact]
    public void Validate_AllowsConfiguredOrigins()
    {
        var cors = new CorsSettings
        {
            AllowedOrigins = new[] { "https://example.com" }
        };

        var exception = Record.Exception(cors.Validate);

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_ThrowsWhenNoOriginsConfigured()
    {
        var cors = new CorsSettings
        {
            AllowedOrigins = Array.Empty<string>()
        };

        var act = () => cors.Validate();

        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Equal("At least one CORS origin must be configured in Cors.AllowedOrigins.", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ThrowsWhenOriginMissing(string origin)
    {
        var cors = new CorsSettings
        {
            AllowedOrigins = new[] { origin }
        };

        var act = () => cors.Validate();

        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Equal("CORS origins cannot be null or whitespace.", exception.Message);
    }
}
