namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.Exceptions;

/// <summary>
///     Thrown by <c>IDynamicNutritionPlanGenerator</c> (A2 part 2 / PR-D2)
///     when the composed risk profile does not contain any triggering
///     threat. The exception is caught at the
///     <c>RecommendDynamicNutritionPlanCommandService</c> boundary and
///     converted into a <c>Result.Failure</c> with the
///     <c>AgronomicErrors.NoTriggeringRisk</c> error constant, so the
///     REST surface sees a normal 4xx response (CC-7: early throw, no
///     silent default).
///     <para>
///         Defined in this PR (A2 part 1) so the per-risk evaluators
///         and the <c>DynamicNutritionPolicy</c> VO compile against a
///         stable exception type, even though the only thrower ships
///         in PR-D2.
///     </para>
/// </summary>
public class DynamicNutritionPlanUnavailableException : Exception
{
    /// <summary>
    ///     Initialises the exception with a human-readable reason.
    /// </summary>
    /// <param name="message">The reason no plan could be generated.</param>
    public DynamicNutritionPlanUnavailableException(string message)
        : base(message)
    {
    }
}
