namespace Ratatosk;

[Trait("Category", "Unit")]
[Trait("Feature", "AuthenticationCredential")]
public class AuthenticationCredentialTests
{
    [Fact]
    public void Should_CreateWithTypeAndValue()
    {
        var cred = new AuthenticationCredential(AuthenticationScheme.ApiKey, "my-key");
        Assert.Equal(AuthenticationScheme.ApiKey, cred.Scheme);
        Assert.Equal("my-key", cred.Value);
        Assert.Null(cred.ExpiresAt);
        Assert.False(cred.IsExpired);
    }

    [Fact]
    public void Should_Throw_When_ValueIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new AuthenticationCredential(AuthenticationScheme.ApiKey, null!));
    }

    [Fact]
    public void Should_Throw_When_ValueIsEmpty()
    {
        Assert.Throws<ArgumentException>(() => new AuthenticationCredential(AuthenticationScheme.ApiKey, ""));
    }

    [Fact]
    public void Should_BeExpired_When_ExpirationPassed()
    {
        var cred = new AuthenticationCredential(AuthenticationScheme.Bearer, "tok", DateTime.UtcNow.AddMinutes(-5));
        Assert.True(cred.IsExpired);
    }

    [Fact]
    public void Should_NotBeExpired_When_ExpirationInFuture()
    {
        var cred = new AuthenticationCredential(AuthenticationScheme.Bearer, "tok", DateTime.UtcNow.AddHours(1));
        Assert.False(cred.IsExpired);
    }

    [Fact]
    public void Should_WillExpireSoon()
    {
        var cred = new AuthenticationCredential(AuthenticationScheme.Bearer, "tok", DateTime.UtcNow.AddSeconds(30));
        Assert.True(cred.WillExpireSoon(TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void Should_NotWillExpireSoon()
    {
        var cred = new AuthenticationCredential(AuthenticationScheme.Bearer, "tok", DateTime.UtcNow.AddHours(2));
        Assert.False(cred.WillExpireSoon(TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void Should_WillExpireSoon_ReturnFalse_WhenNoExpiration()
    {
        var cred = new AuthenticationCredential(AuthenticationScheme.ApiKey, "key");
        Assert.False(cred.WillExpireSoon(TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public void Should_GetTimeUntilExpiration()
    {
        var cred = new AuthenticationCredential(AuthenticationScheme.Bearer, "tok", DateTime.UtcNow.AddMinutes(30));
        var remaining = cred.GetTimeUntilExpiration();
        Assert.NotNull(remaining);
        Assert.True(remaining.Value.TotalMinutes > 28);
    }

    [Fact]
    public void Should_GetTimeUntilExpiration_ReturnNull_WhenNoExpiration()
    {
        var cred = new AuthenticationCredential(AuthenticationScheme.ApiKey, "key");
        Assert.Null(cred.GetTimeUntilExpiration());
    }

    [Fact]
    public void Should_GetTimeUntilExpiration_ReturnZero_WhenExpired()
    {
        var cred = new AuthenticationCredential(AuthenticationScheme.Bearer, "tok", DateTime.UtcNow.AddMinutes(-5));
        var remaining = cred.GetTimeUntilExpiration();
        Assert.NotNull(remaining);
        Assert.Equal(TimeSpan.Zero, remaining.Value);
    }

    [Fact]
    public void Should_CreateTokenCredential()
    {
        var cred = AuthenticationCredential.ForBearerToken("my-token", null, "Bearer");
        Assert.Equal(AuthenticationScheme.Bearer, cred.Scheme);
        Assert.Equal("my-token", cred.Value);
        Assert.Equal("Bearer", cred.Properties["TokenType"]);
    }

    [Fact]
    public void Should_CreateTokenWithExpiration()
    {
        var expires = DateTime.UtcNow.AddHours(1);
        var cred = AuthenticationCredential.ForBearerToken("tok", expires);
        Assert.Equal(expires, cred.ExpiresAt);
    }

    [Fact]
    public void Should_CreateApiKeyCredential()
    {
        var cred = AuthenticationCredential.ForApiKey("api-key-123");
        Assert.Equal(AuthenticationScheme.ApiKey, cred.Scheme);
        Assert.Equal("api-key-123", cred.Value);
    }

    [Fact]
    public void Should_Throw_When_CreateApiKeyWithNull()
    {
        Assert.Throws<ArgumentNullException>(() => AuthenticationCredential.ForApiKey(null!));
    }

    [Fact]
    public void Should_CreateBasicCredential()
    {
        var cred = AuthenticationCredential.ForBasic("user", "pass");
        Assert.Equal(AuthenticationScheme.Basic, cred.Scheme);
        Assert.Equal("user", cred.Properties["Username"]);
        Assert.Equal("pass", cred.Properties["Password"]);
    }

    [Fact]
    public void Should_Throw_When_CreateBasicWithNull()
    {
        Assert.Throws<ArgumentNullException>(() => AuthenticationCredential.ForBasic(null!, "pass"));
        Assert.Throws<ArgumentNullException>(() => AuthenticationCredential.ForBasic("user", null!));
    }

    [Fact]
    public void Should_HaveObtainedAtTimestamp()
    {
        var before = DateTime.UtcNow.AddSeconds(-5);
        var cred = new AuthenticationCredential(AuthenticationScheme.ApiKey, "key");
        var after = DateTime.UtcNow.AddSeconds(5);
        Assert.InRange(cred.ObtainedAt, before, after);
    }
}
