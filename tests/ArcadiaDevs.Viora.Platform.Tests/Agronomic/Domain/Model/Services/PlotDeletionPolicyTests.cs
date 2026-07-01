using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Services;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;
using ArcadiaDevs.Viora.Platform.Shared.Application.Model;
using ArcadiaDevs.Viora.Platform.Shared.Domain.Model;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Domain.Model.Services;

/// <summary>
///     AGRO-013a unit tests for <see cref="PlotDeletionPolicy"/> (A3, the
///     S2.7 + S2.8 spec scenarios). The policy decides whether a plot can
///     be deleted (only active plots), whether the deletion should be
///     logical (deactivate + update) versus physical (remove), and produces
///     a human-readable reason for the rejection when deletion is not
///     allowed. The actual <c>HasRelatedOperationalRecordsAsync</c> check
///     lives on the repository — the policy receives the resolved boolean.
/// </summary>
public class PlotDeletionPolicyTests
{
    private static Plot CreateActivePlot() =>
        ((Result<Plot, Error>.Success)Plot.Create(
            ownerUserId: 1,
            plotName: "Test plot",
            polygonCoordinates: ((Result<PolygonCoordinates, Error>.Success)PolygonCoordinates.Create(new List<GeoPoint>
            {
                ((Result<GeoPoint, Error>.Success)GeoPoint.Create(-12.0m, -77.0m)).Value,
                ((Result<GeoPoint, Error>.Success)GeoPoint.Create(-12.0m, -77.1m)).Value,
                ((Result<GeoPoint, Error>.Success)GeoPoint.Create(-12.1m, -77.1m)).Value,
                ((Result<GeoPoint, Error>.Success)GeoPoint.Create(-12.1m, -77.0m)).Value,
                ((Result<GeoPoint, Error>.Success)GeoPoint.Create(-12.0m, -77.0m)).Value,
            })).Value,
            areaSize: 100m,
            cropType: "Coffee",
            variety: "Arabica",
            location: "Lima",
            campaign: "2026",
            notes: "test")).Value;

    private static PlotDeletionPolicy BuildPolicy() => new();

    // ---------- CanDelete ----------

    [Fact]
    public void CanDelete_ActivePlot_ReturnsTrue()
    {
        // GIVEN an active plot
        var plot = CreateActivePlot();
        var policy = BuildPolicy();

        // WHEN the policy is asked
        // THEN the plot can be deleted
        Assert.True(policy.CanDelete(plot));
    }

    [Fact]
    public void CanDelete_InactivePlot_ReturnsFalse()
    {
        // GIVEN a deactivated plot
        var plot = CreateActivePlot();
        plot.Deactivate();
        var policy = BuildPolicy();

        // WHEN the policy is asked
        // THEN the plot CANNOT be deleted (already inactive; double-delete
        // would be a no-op or worse, would erase audit history)
        Assert.False(policy.CanDelete(plot));
    }

    [Fact]
    public void CanDelete_NullPlot_ReturnsFalse()
    {
        // GIVEN a null plot
        var policy = BuildPolicy();

        // WHEN the policy is asked
        // THEN false is returned (defensive: null is not deletable)
        Assert.False(policy.CanDelete(null));
    }

    // ---------- RequiresLogicalDeletion (S2.7 + S2.8) ----------

    [Fact]
    public void RequiresLogicalDeletion_HasRelatedOperationalRecords_ReturnsTrue()
    {
        // GIVEN the policy and a true "has related records" signal (e.g. the plot
        //   has a DynamicNutritionPlan, an IoTDevice, an AgronomicStatistic, etc.)
        var policy = BuildPolicy();

        // WHEN the policy is asked
        // THEN logical deletion is required (deactivate, not remove — preserves
        //   the audit trail of the related operational records)
        Assert.True(policy.RequiresLogicalDeletion(hasRelatedOperationalRecords: true));
    }

    [Fact]
    public void RequiresLogicalDeletion_NoRelatedRecords_ReturnsFalse()
    {
        // GIVEN the policy and a false "has related records" signal
        var policy = BuildPolicy();

        // WHEN the policy is asked
        // THEN physical deletion is allowed (no audit trail to preserve)
        Assert.False(policy.RequiresLogicalDeletion(hasRelatedOperationalRecords: false));
    }

    // ---------- ExplainDeletionRejection ----------

    [Fact]
    public void ExplainDeletionRejection_NullPlot_ReturnsRequiredMessage()
    {
        // GIVEN a null plot
        var policy = BuildPolicy();

        // WHEN the policy is asked
        // THEN the rejection reason references the required plot
        var reason = policy.ExplainDeletionRejection(null);
        Assert.Contains("required", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExplainDeletionRejection_InactivePlot_ReturnsActiveMessage()
    {
        // GIVEN an inactive plot
        var plot = CreateActivePlot();
        plot.Deactivate();
        var policy = BuildPolicy();

        // WHEN the policy is asked
        // THEN the rejection reason explains that only active plots can be deleted
        var reason = policy.ExplainDeletionRejection(plot);
        Assert.Contains("active", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExplainDeletionRejection_ActivePlot_ReturnsGenericCannotDelete()
    {
        // GIVEN an active plot (the only path where deletion is allowed;
        //   the rejection reason is the "no other reason" path)
        var plot = CreateActivePlot();
        var policy = BuildPolicy();

        // WHEN the policy is asked
        // THEN the rejection reason is the generic "cannot be deleted" message
        //   (note: in the normal flow, this branch is unreachable because
        //   CanDelete(plot) returns true; the message is for the "guard
        //   fired unexpectedly" debug case)
        var reason = policy.ExplainDeletionRejection(plot);
        Assert.Contains("cannot be deleted", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PlotDeactivate_FlipsIsActiveAndIsDeleted()
    {
        // GIVEN an active plot
        var plot = CreateActivePlot();
        Assert.True(plot.IsActive);
        Assert.False(plot.IsDeleted);

        // WHEN the plot is deactivated
        plot.Deactivate();

        // THEN both IsActive and IsDeleted are flipped (the logical-delete sentinel)
        Assert.False(plot.IsActive);
        Assert.True(plot.IsDeleted);
    }
}
