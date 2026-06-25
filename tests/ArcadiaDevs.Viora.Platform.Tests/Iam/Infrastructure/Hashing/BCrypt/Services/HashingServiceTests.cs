using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Hashing.BCrypt.Services;

namespace ArcadiaDevs.Viora.Platform.Tests.Iam.Infrastructure.Hashing.BCrypt.Services;

public class HashingServiceTests
{
    private readonly HashingService _sut = new();

    [Fact]
    public void HashPassword_ProducesNonEmptyString_DifferentForEachCall()
    {
        // GIVEN a HashingService
        var hash1 = _sut.HashPassword("same-input");
        var hash2 = _sut.HashPassword("same-input");

        // THEN non-empty
        Assert.False(string.IsNullOrEmpty(hash1));
        // AND different from input
        Assert.NotEqual("same-input", hash1);
        // AND two calls with the same input produce different hashes (BCrypt salt)
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_ReturnsTrue_ForCorrectPassword()
    {
        // GIVEN a hash of "P@ssw0rd!"
        var hash = _sut.HashPassword("P@ssw0rd!");

        // WHEN verifying the original password
        var result = _sut.VerifyPassword("P@ssw0rd!", hash);

        // THEN the result is true
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_ReturnsFalse_ForWrongPassword()
    {
        // GIVEN a hash of "P@ssw0rd!"
        var hash = _sut.HashPassword("P@ssw0rd!");

        // WHEN verifying a wrong password
        var result = _sut.VerifyPassword("not-the-password", hash);

        // THEN the result is false
        Assert.False(result);
    }
}
