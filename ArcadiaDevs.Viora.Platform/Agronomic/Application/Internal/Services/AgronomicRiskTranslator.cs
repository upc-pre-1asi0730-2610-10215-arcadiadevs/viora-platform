using System.Collections.Generic;
using ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Application.Internal.Services;

/// <summary>
///     Pure translator implementation. Maps the per-risk boolean signals
///     + the snapshot's <see cref="ClimateRiskLevel"/> onto the
///     <see cref="EThreatType"/> set consumed by
///     <c>IDynamicNutritionPlanGenerator</c> (A2 part 2 / PR-D2).
///     <para>
///         Mapping rules (design §5.2.2, engram #45):
///         <list type="bullet">
///             <item>
///                 <c>ClimateRiskLevel.High</c> → <see cref="EThreatType.ClimateHigh"/>
///                 (also fires when <c>Critical</c> is observed, matching
///                 the OS's "High and Extreme are both >= High" semantics).
///             </item>
///             <item>
///                 <c>ClimateRiskLevel.Critical</c> → <see cref="EThreatType.ClimateExtreme"/>
///                 (the OS "EXTREME" → "Critical" alias).
///             </item>
///             <item>
///                 <c>chillDeficit == true</c> → <see cref="EThreatType.ChillDeficit"/>.
///             </item>
///             <item>
///                 <c>lowNdvi == true</c> → <see cref="EThreatType.LowNdvi"/>.
///             </item>
///             <item>
///                 <c>hydricStress == true</c> → <see cref="EThreatType.HydricStress"/>.
///             </item>
///         </list>
///     </para>
///     <para>
///         ClimateHigh AND ClimateExtreme may BOTH be present when the
///         level is <c>Critical</c> (the OS pattern: "Critical includes
///         both High and Extreme"). The OS's translator marks both;
///         matching that keeps the rationale summary's risk list
///         accurate (the audit log will show <c>ClimateHigh, ClimateExtreme</c>
///         for a Critical snapshot, matching the OS goldens).
///     </para>
///     <para>
///         Defensive: <c>Medium</c>/<c>Low</c> climate levels do NOT
///         produce a climate risk on their own; the 3 per-risk
///         evaluators carry the load. This is the relaxation locked in
///         decision #1: the OS would refuse to generate a plan on
///         ClimateMedium (only HIGH/EXTREME fired), but the WA can
///         still generate one on ChillDeficit alone, LowNdvi alone,
///         or HydricStress alone (the 4-risk trigger expansion).
///     </para>
/// </summary>
public sealed class AgronomicRiskTranslator : IAgronomicRiskTranslator
{
    /// <inheritdoc />
    public IReadOnlyCollection<EThreatType> Translate(
        ClimateRiskLevel climate,
        bool chillDeficit,
        bool lowNdvi,
        bool hydricStress)
    {
        var risks = new List<EThreatType>(capacity: 5);

        switch (climate)
        {
            case ClimateRiskLevel.Critical:
                // Critical carries both High and Extreme (OS pattern).
                risks.Add(EThreatType.ClimateHigh);
                risks.Add(EThreatType.ClimateExtreme);
                break;

            case ClimateRiskLevel.High:
                risks.Add(EThreatType.ClimateHigh);
                break;

            case ClimateRiskLevel.Medium:
            case ClimateRiskLevel.Low:
            default:
                // No climate risk; the 3 per-risk evaluators carry the load.
                break;
        }

        if (chillDeficit)
        {
            risks.Add(EThreatType.ChillDeficit);
        }

        if (lowNdvi)
        {
            risks.Add(EThreatType.LowNdvi);
        }

        if (hydricStress)
        {
            risks.Add(EThreatType.HydricStress);
        }

        return risks;
    }
}
