using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using IdentityAuth.Application.Common.Settings;
using IdentityAuth.Domain.Entities;
using IdentityAuth.Infrastructure.Security;

namespace IdentityAuth.UnitTests;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _jwtTokenService;
    private readonly JwtSettings _jwtSettings;

    public JwtTokenServiceTests()
    {
        _jwtSettings = new JwtSettings
        {
            SecretKey = "TestSecretKeyThatIsAtLeast32CharactersLong!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            AccessTokenExpirationMinutes = 15
        };
        _jwtTokenService = new JwtTokenService(_jwtSettings);
    }

    private User CreateTestUser()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "John",
            LastName = "Doe",
            UserRoles = new List<UserRole>
            {
                new UserRole
                {
                    Role = new Role { Name = "User", NormalizedName = "USER" }
                }
            }
        };
    }

    [Fact]
    public void GenerateAccessToken_ReturnsNonEmptyString()
    {
        var user = CreateTestUser();

        var token = _jwtTokenService.GenerateAccessToken(user);

        Assert.False(string.IsNullOrEmpty(token));
    }

    [Fact]
    public void GenerateAccessToken_ReturnsValidJwt()
    {
        var user = CreateTestUser();

        var token = _jwtTokenService.GenerateAccessToken(user);
        var handler = new JwtSecurityTokenHandler();

        Assert.True(handler.CanReadToken(token));
    }

    [Fact]
    public void GenerateAccessToken_ContainsUserId()
    {
        var user = CreateTestUser();

        var token = _jwtTokenService.GenerateAccessToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub);
        Assert.NotNull(subClaim);
        Assert.Equal(user.Id.ToString(), subClaim.Value);
    }

    [Fact]
    public void GenerateAccessToken_ContainsEmailClaim()
    {
        var user = CreateTestUser();

        var token = _jwtTokenService.GenerateAccessToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
        Assert.NotNull(emailClaim);
        Assert.Equal(user.Email, emailClaim.Value);
    }

    [Fact]
    public void GenerateAccessToken_ContainsRoleClaim()
    {
        var user = CreateTestUser();

        var token = _jwtTokenService.GenerateAccessToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);
        Assert.NotNull(roleClaim);
        Assert.Equal("User", roleClaim.Value);
    }

    [Fact]
    public void GenerateAccessToken_ContainsCorrectIssuer()
    {
        var user = CreateTestUser();

        var token = _jwtTokenService.GenerateAccessToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Equal(_jwtSettings.Issuer, jwtToken.Issuer);
    }

    [Fact]
    public void GenerateAccessToken_ContainsCorrectAudience()
    {
        var user = CreateTestUser();

        var token = _jwtTokenService.GenerateAccessToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.Contains(_jwtSettings.Audience, jwtToken.Audiences);
    }

    [Fact]
    public void GenerateAccessToken_HasExpiration()
    {
        var user = CreateTestUser();

        var token = _jwtTokenService.GenerateAccessToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        Assert.NotNull(jwtToken.ValidTo);
        Assert.True(jwtToken.ValidTo > DateTime.UtcNow);
    }

    [Fact]
    public void GenerateAccessToken_ExpirationMatchesSettings()
    {
        var user = CreateTestUser();

        var token = _jwtTokenService.GenerateAccessToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var expectedExpiry = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);
        var diff = Math.Abs((jwtToken.ValidTo - expectedExpiry).TotalSeconds);

        Assert.True(diff < 5, "Token expiration should match configured settings within 5 seconds tolerance");
    }

    [Fact]
    public void GenerateAccessToken_ContainsJtiClaim()
    {
        var user = CreateTestUser();

        var token = _jwtTokenService.GenerateAccessToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var jtiClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti);
        Assert.NotNull(jtiClaim);
        Assert.False(string.IsNullOrEmpty(jtiClaim.Value));
    }

    [Fact]
    public void GenerateAccessToken_DifferentUsers_DifferentTokens()
    {
        var user1 = CreateTestUser();
        var user2 = CreateTestUser();
        user2.Id = Guid.NewGuid();

        var token1 = _jwtTokenService.GenerateAccessToken(user1);
        var token2 = _jwtTokenService.GenerateAccessToken(user2);

        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void GenerateAccessToken_MultipleRoles_ContainsAllRoles()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.com",
            FirstName = "Admin",
            LastName = "User",
            UserRoles = new List<UserRole>
            {
                new UserRole { Role = new Role { Name = "User", NormalizedName = "USER" } },
                new UserRole { Role = new Role { Name = "Admin", NormalizedName = "ADMIN" } }
            }
        };

        var token = _jwtTokenService.GenerateAccessToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var roleClaims = jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role).ToList();
        Assert.Equal(2, roleClaims.Count);
        Assert.Contains(roleClaims, c => c.Value == "User");
        Assert.Contains(roleClaims, c => c.Value == "Admin");
    }
}
