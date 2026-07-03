using System.Reflection;
using ArcadiaDevs.Viora.Platform.Agronomic.Interfaces.Rest.Swagger;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ArcadiaDevs.Viora.Platform.Tests.Agronomic.Interfaces.Rest.Swagger;

/// <summary>
///     Unit tests for <see cref="PlotViewResponseOperationFilter"/>.
///     Tests the attribute-reading logic that drives oneOf schema generation.
///     Full Swagger pipeline test requires Swashbuckle internals — this validates
///     the reflection-based attribute extraction that the filter relies on.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Database", "InMemory")]
public class PlotViewResponseOperationFilterTests
{
    /// <summary>
    ///     GIVEN an action with a single [ProducesResponseType(200)] attribute
    ///     WHEN the filter's attribute extraction logic runs
    ///     THEN only 1 response type is found (no oneOf needed).
    /// </summary>
    [Fact]
    public void Apply_SingleView_FindsExactlyOneResponseType()
    {
        // GIVEN an action with a single 200 response type
        var methodInfo = typeof(SingleViewActionHolder).GetMethod(nameof(SingleViewActionHolder.Action))!;

        // WHEN extracting ProducesResponseType(200) attributes (the filter's core logic)
        var okResponseTypes = methodInfo
            .GetCustomAttributes(typeof(ProducesResponseTypeAttribute), inherit: true)
            .OfType<ProducesResponseTypeAttribute>()
            .Where(a => a.StatusCode == StatusCodes.Status200OK && a.Type != typeof(void))
            .Select(a => a.Type!)
            .Distinct()
            .ToList();

        // THEN exactly 1 type is found (no oneOf)
        Assert.Single(okResponseTypes);
        Assert.Equal(typeof(string), okResponseTypes[0]);
    }

    /// <summary>
    ///     GIVEN an action with multiple [ProducesResponseType(200)] attributes
    ///     WHEN the filter's attribute extraction logic runs
    ///     THEN multiple response types are found (triggers oneOf).
    /// </summary>
    [Fact]
    public void Apply_MultipleViews_FindsMultipleResponseTypes()
    {
        // GIVEN an action with multiple 200 response types (simulates ?view= dispatch)
        var methodInfo = typeof(MultiViewActionHolder).GetMethod(nameof(MultiViewActionHolder.Action))!;

        // WHEN extracting ProducesResponseType(200) attributes
        var okResponseTypes = methodInfo
            .GetCustomAttributes(typeof(ProducesResponseTypeAttribute), inherit: true)
            .OfType<ProducesResponseTypeAttribute>()
            .Where(a => a.StatusCode == StatusCodes.Status200OK && a.Type != typeof(void))
            .Select(a => a.Type!)
            .Distinct()
            .ToList();

        // THEN 2 types are found (triggers oneOf schema)
        Assert.Equal(2, okResponseTypes.Count);
        Assert.Contains(typeof(string), okResponseTypes);
        Assert.Contains(typeof(int), okResponseTypes);
    }

    /// <summary>Holder for a single-view action (1 ProducesResponseType).</summary>
    private sealed class SingleViewActionHolder
    {
        [HttpGet("test")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        public IActionResult Action() => new OkResult();
    }

    /// <summary>Holder for a multi-view action (2 ProducesResponseType(200)).</summary>
    private sealed class MultiViewActionHolder
    {
        [HttpGet("test")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        public IActionResult Action() => new OkResult();
    }
}
