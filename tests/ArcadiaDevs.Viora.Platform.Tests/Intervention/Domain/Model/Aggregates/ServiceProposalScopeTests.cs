using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.Aggregates;
using ArcadiaDevs.Viora.Platform.Intervention.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Tests.Intervention.Domain.Model.Aggregates;

/// <summary>
///     Coverage for <see cref="ServiceProposal"/>'s <c>Scope</c> constructor
///     argument — structured as an <see cref="IReadOnlyList{T}"/> of strings
///     rather than a single opaque string (field-level parity fix). The
///     constructor rejects a null/empty list and a list containing any
///     blank item, but does not deduplicate.
/// </summary>
public class ServiceProposalScopeTests
{
    private static readonly DateOnly ProposedDate = new(2026, 7, 5);

    private static CostEstimate ValidCostEstimate() => new(100m, "USD");

    private static ServiceProposal CreateProposal(IReadOnlyList<string> scope) =>
        new(
            interventionRequestId: 1,
            specialistId: 1,
            serviceTitle: "Pest control",
            durationLabel: "2 weeks",
            scope: scope,
            proposedDate: ProposedDate,
            costEstimate: ValidCostEstimate(),
            proposalDetails: "Full treatment plan");

    [Fact]
    public void Constructor_ValidScopeList_SetsScope()
    {
        // GIVEN a non-empty list of non-blank scope items
        var scope = new List<string> { "Inspection", "Treatment", "Follow-up" };

        // WHEN the ServiceProposal is constructed
        var proposal = CreateProposal(scope);

        // THEN Scope is set as provided, preserving order
        Assert.Equal(scope, proposal.Scope);
    }

    [Fact]
    public void Constructor_ScopeWithDuplicateItems_IsAllowed()
    {
        // GIVEN a scope list with duplicate items
        var scope = new List<string> { "Inspection", "Inspection" };

        // WHEN the ServiceProposal is constructed
        var proposal = CreateProposal(scope);

        // THEN duplicates are preserved — no dedup logic in the aggregate
        Assert.Equal(2, proposal.Scope.Count);
    }

    [Fact]
    public void Constructor_NullScope_ThrowsArgumentException()
    {
        // GIVEN a null scope list
        // WHEN/THEN constructing the ServiceProposal throws
        var ex = Assert.Throws<ArgumentException>(() => CreateProposal(null!));
        Assert.Equal("scope", ex.ParamName);
    }

    [Fact]
    public void Constructor_EmptyScope_ThrowsArgumentException()
    {
        // GIVEN an empty scope list
        // WHEN/THEN constructing the ServiceProposal throws
        var ex = Assert.Throws<ArgumentException>(() => CreateProposal(Array.Empty<string>()));
        Assert.Equal("scope", ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ScopeContainingBlankItem_ThrowsArgumentException(string blankItem)
    {
        // GIVEN a scope list containing a blank item alongside valid ones
        var scope = new List<string> { "Inspection", blankItem };

        // WHEN/THEN constructing the ServiceProposal throws
        var ex = Assert.Throws<ArgumentException>(() => CreateProposal(scope));
        Assert.Equal("scope", ex.ParamName);
    }

    [Fact]
    public void Constructor_ScopeContainingNullItem_ThrowsArgumentException()
    {
        // GIVEN a scope list containing a null item
        var scope = new List<string> { "Inspection", null! };

        // WHEN/THEN constructing the ServiceProposal throws
        var ex = Assert.Throws<ArgumentException>(() => CreateProposal(scope));
        Assert.Equal("scope", ex.ParamName);
    }
}
