using System.Net;
using System.Net.Http.Json;
using IdentityAuth.Application.Authentication.DTOs;

namespace IdentityAuth.IntegrationTests;

public class RegistrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RegistrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidRequest_Returns201()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "newuser@example.com",
            Password = "StrongPass1!",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsUserData()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "userdata@example.com",
            Password = "StrongPass1!",
            FirstName = "Jane",
            LastName = "Smith"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        var content = await response.Content.ReadFromJsonAsync<RegisterResponse>();

        // Assert
        Assert.NotNull(content);
        Assert.Equal("userdata@example.com", content.Email);
        Assert.Equal("Jane", content.FirstName);
        Assert.Equal("Smith", content.LastName);
        Assert.NotEqual(Guid.Empty, content.Id);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "duplicate@example.com",
            Password = "StrongPass1!",
            FirstName = "First",
            LastName = "User"
        };

        // Register first time
        await _client.PostAsJsonAsync("/api/auth/register", request);

        // Act - register again with same email
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_CaseInsensitive_Returns409()
    {
        // Arrange
        var request1 = new RegisterRequest
        {
            Email = "casetest@example.com",
            Password = "StrongPass1!",
            FirstName = "First",
            LastName = "User"
        };

        var request2 = new RegisterRequest
        {
            Email = "CASETEST@EXAMPLE.COM",
            Password = "StrongPass1!",
            FirstName = "Second",
            LastName = "User"
        };

        // Register first time
        await _client.PostAsJsonAsync("/api/auth/register", request1);

        // Act - register with different case
        var response = await _client.PostAsJsonAsync("/api/auth/register", request2);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("weak")]
    [InlineData("nouppercase1!")]
    [InlineData("NOLOWERCASE1!")]
    [InlineData("NoDigits!!")]
    [InlineData("NoSpecial1x")]
    public async Task Register_WeakPassword_Returns400(string password)
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "weakpass@example.com",
            Password = password,
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    [InlineData("@missing.com")]
    public async Task Register_InvalidEmail_Returns400(string email)
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = email,
            Password = "StrongPass1!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_MissingEmail_Returns400()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "",
            Password = "StrongPass1!",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_MissingPassword_Returns400()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "nopass@example.com",
            Password = "",
            FirstName = "Test",
            LastName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_MissingFirstName_Returns400()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "nofirst@example.com",
            Password = "StrongPass1!",
            FirstName = "",
            LastName = "User"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_MissingLastName_Returns400()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "nolast@example.com",
            Password = "StrongPass1!",
            FirstName = "Test",
            LastName = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
