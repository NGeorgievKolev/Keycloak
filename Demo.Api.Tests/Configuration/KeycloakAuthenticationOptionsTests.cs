using Demo.Api.Configuration;
using Xunit;

namespace Demo.Api.Tests.Configuration;

public class KeycloakAuthenticationOptionsTests
{
    [Fact]
    public void Validate_AllowsValidConfiguration()
    {
        var options = new KeycloakAuthenticationOptions
        {
            Authority = "https://example.com/auth",
            Audience = "demo-api",
            NameClaimType = "preferred_username",
            RoleClaimType = "roles"
        };

        var exception = Record.Exception(options.Validate);

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-uri")]
    public void Validate_ThrowsWhenAuthorityIsInvalid(string authority)
    {
        var options = new KeycloakAuthenticationOptions
        {
            Authority = authority,
            Audience = "demo-api"
        };

        var act = () => options.Validate();

        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Equal("Authentication.Authority must be an absolute URI.", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ThrowsWhenAudienceMissing(string audience)
    {
        var options = new KeycloakAuthenticationOptions
        {
            Authority = "https://example.com/auth",
            Audience = audience
        };

        var act = () => options.Validate();

        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Equal("Authentication.Audience must be provided.", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ThrowsWhenNameClaimTypeMissing(string? nameClaimType)
    {
        var options = new KeycloakAuthenticationOptions
        {
            Authority = "https://example.com/auth",
            Audience = "demo-api",
            NameClaimType = nameClaimType!
        };

        var act = () => options.Validate();

        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Equal("Authentication.NameClaimType must be provided.", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ThrowsWhenRoleClaimTypeMissing(string? roleClaimType)
    {
        var options = new KeycloakAuthenticationOptions
        {
            Authority = "https://example.com/auth",
            Audience = "demo-api",
            RoleClaimType = roleClaimType!
        };

        var act = () => options.Validate();

        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Equal("Authentication.RoleClaimType must be provided.", exception.Message);
    }
}
