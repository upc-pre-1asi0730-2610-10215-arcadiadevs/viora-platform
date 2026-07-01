using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Tests.TestHarness;
using Xunit;

namespace ArcadiaDevs.Viora.Platform.Tests.Shared.Infrastructure;

/// <summary>
///     Unit tests for <see cref="TestAuthHelper"/> + the test
///     authentication middleware. Verifies that the helper's
///     <c>WithTestUser</c> extension sets the request headers that
///     the test middleware reads to populate
///     <c>HttpContext.Items["User"]</c> and <c>HttpContext.User</c>.
/// </summary>
[Trait("Category", "Unit")]
public class TestAuthHelperTests
{
    [Fact]
    public void WithTestUser_AddsXTestUserIdHeader_WithId()
    {
        // Arrange
        using var client = new HttpClient();

        // Act
        client.WithTestUser(userId: 42, username: "alice");

        // Assert
        Assert.True(client.DefaultRequestHeaders.Contains(TestAuthHelper.UserIdHeader),
            $"Client should carry '{TestAuthHelper.UserIdHeader}' header.");
        Assert.Equal("42", client.DefaultRequestHeaders.GetValues(TestAuthHelper.UserIdHeader).Single());
    }

    [Fact]
    public void WithTestUser_AddsXTestUsernameHeader_WithUsername()
    {
        // Arrange
        using var client = new HttpClient();

        // Act
        client.WithTestUser(userId: 1, username: "bob");

        // Assert
        Assert.True(client.DefaultRequestHeaders.Contains(TestAuthHelper.UsernameHeader));
        Assert.Equal("bob", client.DefaultRequestHeaders.GetValues(TestAuthHelper.UsernameHeader).Single());
    }

    [Fact]
    public void WithTestUser_WithRole_AddsXTestRolesHeader_AsCsv()
    {
        // Arrange
        using var client = new HttpClient();

        // Act
        client.WithTestUser(userId: 1, username: "alice", roles: new[] { "ADMIN", "FARMER" });

        // Assert
        Assert.True(client.DefaultRequestHeaders.Contains(TestAuthHelper.RolesHeader));
        var csv = client.DefaultRequestHeaders.GetValues(TestAuthHelper.RolesHeader).Single();
        Assert.Equal("ADMIN,FARMER", csv);
    }

    [Fact]
    public void WithTestUser_WithoutRoles_OmitsXTestRolesHeader()
    {
        // Arrange
        using var client = new HttpClient();

        // Act
        client.WithTestUser(userId: 1, username: "alice");

        // Assert — no roles header when no roles specified
        Assert.False(client.DefaultRequestHeaders.Contains(TestAuthHelper.RolesHeader));
    }
}
