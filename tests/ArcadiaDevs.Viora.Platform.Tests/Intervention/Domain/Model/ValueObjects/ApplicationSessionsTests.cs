using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Tests.Intervention.Domain.Model.ValueObjects;

/// <summary>
///     Coverage for <see cref="ApplicationSessions"/> — the number of
///     application sessions for a single <see cref="PrescribedProduct"/>
///     (REQ-TP-3). The constructor requires a strictly positive count.
/// </summary>
public class ApplicationSessionsTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(100)]
    public void Constructor_ValidCount_CreatesApplicationSessions(int count)
    {
        // GIVEN a positive count
        // WHEN the ApplicationSessions is constructed
        var sessions = new ApplicationSessions(count);

        // THEN Count is set as provided
        Assert.Equal(count, sessions.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public void Constructor_NonPositiveCount_ThrowsArgumentException(int count)
    {
        // GIVEN a non-positive count
        // WHEN/THEN constructing ApplicationSessions throws
        var ex = Assert.Throws<ArgumentException>(() => new ApplicationSessions(count));
        Assert.Equal("count", ex.ParamName);
    }
}
