using System.Security.Claims;
using System.Text;
using ArcadiaDevs.Viora.Platform.Iam.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Tokens.Jwt.Configuration;
using ArcadiaDevs.Viora.Platform.Iam.Infrastructure.Tokens.Jwt.Services;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;
using ArcadiaDevs.Viora.Platform.Tests.TestHarness;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

// Claims in the JWT are serialized in URI form because TokenService passes
// ClaimTypes.Sid / ClaimTypes.Name / ClaimTypes.Role to JsonWebTokenHandler
// with the default OutboundClaimTypeMap (which is empty). Do not "fix" the
// test to expect short names ("sid", "unique_name", "role") — see
// engram obs #132 for the gotcha. Round-trip (sid value) is the same
// regardless of claim-type key.

namespace ArcadiaDevs.Viora.Platform.Tests.Iam.Infrastructure.Tokens.Jwt.Services;

public class TokenServiceTests
{
    // 35-byte ASCII secret — required for HS256 (>= 32 bytes)
    private const string TestSecret = "dev-only-test-secret-32-bytes-min!!";

    private static IOptions<TokenSettings> Options() =>
        // Fully-qualified: Microsoft.Extensions.Options is NOT in the implicit-usings list.
        Microsoft.Extensions.Options.Options.Create(new TokenSettings { Secret = TestSecret });

    private readonly TokenService _sut = new(Options());

    [Fact]
    public void GenerateToken_IncludesSidNameAndRoleClaims_AndExpiresIn7Days()
    {
        // GIVEN a user with id, username, and two roles
        var user = new User("alice", "irrelevant-hash");
        var grower = ((Result<Role, Error>.Success)Role.Create("Grower")).Value;
        var specialist = ((Result<Role, Error>.Success)Role.Create("Specialist")).Value;
        user.Roles.Add(grower);
        user.Roles.Add(specialist);

        // WHEN generating a token
        var before = DateTime.UtcNow;
        var token = _sut.GenerateToken(user);
        var after = DateTime.UtcNow;

        // THEN the token decodes to expected claims
        var jwt = new JsonWebToken(token);

        Assert.Equal(user.Id.ToString(), jwt.Claims.First(c => c.Type == ClaimTypes.Sid).Value);
        Assert.Equal("alice", jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value);

        var roleClaims = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Equal(2, roleClaims.Count);
        Assert.Contains("Grower", roleClaims);
        Assert.Contains("Specialist", roleClaims);

        // AND the exp claim falls within [now + 6d23h, now + 7d + 5s]
        var expSeconds = long.Parse(jwt.Claims.First(c => c.Type == "exp").Value);
        var exp = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;
        Assert.InRange(exp, before.AddDays(6).AddHours(23), after.AddDays(7).AddSeconds(5));
    }

    [Fact]
    public async Task ValidateToken_ReturnsSuccess_ForTokenJustGenerated()
    {
        var user = new User("alice", "x");
        var token = _sut.GenerateToken(user);

        var result = await _sut.ValidateToken(token);

        Assert.True(result.IsValid);
        Assert.Equal(user.Id, result.UserId);
        Assert.Null(result.FailureCode);
    }

    [Fact]
    public async Task ValidateToken_ReturnsExpired_WhenTokenHasPastExpiry()
    {
        // Build a JWT with a past expiry directly. Production GenerateToken
        // hardcodes Expires = UtcNow.AddDays(7) (TokenService.cs:51), so this
        // is the only way to obtain a past-expiry token. See obs #132.
        var key = Encoding.ASCII.GetBytes(TestSecret);
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Sid, "42"),
                new Claim(ClaimTypes.Name, "alice"),
            }),
            // NotBefore and IssuedAt MUST be set in the past so the lifetime
            // ordering (nbf <= iat < exp) is valid; otherwise the handler
            // throws SecurityTokenInvalidLifetimeException instead of
            // SecurityTokenExpiredException. See engram obs #140 (sdd-verify
            // of testing-xunit-bootstrap, 2026-06-27).
            NotBefore = DateTime.UtcNow.AddSeconds(-20),
            IssuedAt   = DateTime.UtcNow.AddSeconds(-20),
            Expires    = DateTime.UtcNow.AddSeconds(-10),   // already expired
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
        };
        var handler = new JsonWebTokenHandler();
        var expiredToken = handler.CreateToken(descriptor);

        var result = await _sut.ValidateToken(expiredToken);

        Assert.False(result.IsValid);
        Assert.Equal("Iam.TokenExpired", result.FailureCode);   // hardcoded in JwtValidationResult.Expired
    }

    [Fact]
    public async Task ValidateToken_ReturnsInvalid_ForTamperedSignature()
    {
        var user = new User("alice", "x");
        var token = _sut.GenerateToken(user);
        // Replace the last character with a different one — flattens the HMAC.
        var tampered = token[..^1] + (token[^1] == 'A' ? 'B' : 'A');

        var result = await _sut.ValidateToken(tampered);

        Assert.False(result.IsValid);
        Assert.Equal("Iam.TokenInvalid", result.FailureCode);
    }

    [Fact]
    public async Task ValidateToken_ReturnsInvalid_ForEmptyString()
    {
        var result = await _sut.ValidateToken("");

        // Strengthened from v1.1: assert the specific FailureCode value, not just non-null.
        // The empty-string and null paths both go through TokenService.cs:71 (the
        // IsNullOrEmpty early-return), which returns JwtValidationResult.Invalid with
        // FailureCode == "Iam.TokenInvalid". Asserting on the exact code catches a
        // regression that changes the FailureCode value (e.g. accidentally returning
        // a different IamErrors.* constant, or null), which the v1.1
        // "Assert.NotNull(result.FailureCode)" assertion would have missed.
        Assert.False(result.IsValid);
        Assert.Equal("Iam.TokenInvalid", result.FailureCode);
    }

    [Fact]
    public async Task ValidateToken_ReturnsInvalid_ForNull()
    {
        // null-forgiving; the production code null-check at TokenService.cs:71 is the thing under test
        var result = await _sut.ValidateToken(null!);

        // Strengthened from v1.1: same rationale as test method 5 above.
        Assert.False(result.IsValid);
        Assert.Equal("Iam.TokenInvalid", result.FailureCode);
    }

    // The two tests below use the real (IOptions<TokenSettings>, IClock) constructor —
    // NOT the backward-compatible one-arg constructor used by `_sut` above, which
    // defaults to SystemClock and would defeat the point of these tests: proving
    // ValidateToken's expiry check is actually driven by the injected IClock rather
    // than falling back to real wall-clock time inside JsonWebTokenHandler.
    //
    // The clock is seeded 1 hour ahead of the real wall clock. JsonWebTokenHandler.
    // CreateToken auto-stamps `nbf`/`iat` from the REAL DateTime.UtcNow whenever the
    // SecurityTokenDescriptor doesn't set NotBefore/IssuedAt explicitly (see the
    // ValidateToken_ReturnsExpired_WhenTokenHasPastExpiry comment above and engram obs
    // #132) — GenerateToken never sets them. Seeding 1 hour ahead keeps the fake
    // "now" safely past that real `nbf` so the lifetime validator's not-yet-valid
    // check never fires because of timing skew between capturing the seed and the
    // token actually being minted a few milliseconds later.

    [Fact]
    public async Task ValidateToken_WithInjectedClock_ReturnsValid_BeforeExpiry()
    {
        var clock = new FakeClock(DateTime.UtcNow.AddHours(1));
        var sut = new TokenService(Options(), clock);
        var user = new User("alice", "x");

        var token = sut.GenerateToken(user);

        // No time advance: still well within the 7-day expiry window.
        var result = await sut.ValidateToken(token);

        Assert.True(result.IsValid);
        Assert.Equal(user.Id, result.UserId);
    }

    [Fact]
    public async Task ValidateToken_WithInjectedClock_ReturnsExpired_WhenClockAdvancesPast7DayWindow()
    {
        var clock = new FakeClock(DateTime.UtcNow.AddHours(1));
        var sut = new TokenService(Options(), clock);
        var user = new User("alice", "x");

        var token = sut.GenerateToken(user);

        // Advance the SAME clock instance (the one TokenService holds a reference to)
        // past the 7-day expiry baked into the token at generation time.
        clock.Advance(TimeSpan.FromDays(7).Add(TimeSpan.FromMinutes(1)));

        var result = await sut.ValidateToken(token);

        Assert.False(result.IsValid);
        Assert.Equal("Iam.TokenExpired", result.FailureCode);
    }
}
