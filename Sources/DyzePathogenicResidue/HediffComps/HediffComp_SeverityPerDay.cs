using Verse;

namespace Dyze.RimWorld.PathogenicResidue
{
    /// <summary>
    /// HediffComp that provides simple severity progression over time.
    /// 
    /// Unlike HediffComp_Immunizable, this does NOT hook into the vanilla disease widget
    /// or immunity system. It adds a fixed amount of severity each interval.
    /// When the configured max severity is reached, the disease removes itself.
    /// </summary>
    public class HediffComp_SeverityPerDay : HediffComp
    {
        internal HediffCompProperties_SeverityPerDay Props => (HediffCompProperties_SeverityPerDay)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (Props.severityPerDay <= 0f)
            {
                return;
            }

            float severityPerTick = Props.severityPerDay / 60000f;
            float targetSeverity = parent.Severity + severityAdjustment + severityPerTick;

            if (Props.maxSeverity > 0f && targetSeverity >= Props.maxSeverity)
            {
                Pawn pawn = parent.pawn;
                if (pawn?.health != null)
                {
                    pawn.health.RemoveHediff(parent);
                }
                return;
            }

            if (severityPerTick > 0f)
            {
                severityAdjustment += severityPerTick;
            }
        }

        public override string CompDebugString()
        {
            return $"severityPerDay: {Props.severityPerDay}\n" +
                   $"maxSeverity: {Props.maxSeverity}\n" +
                   $"currentSeverity: {parent.Severity:F3}";
        }
    }
}
