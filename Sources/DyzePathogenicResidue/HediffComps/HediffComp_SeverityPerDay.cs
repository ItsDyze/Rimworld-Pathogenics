using Verse;

namespace Dyze.RimWorld.PathogenicResidue
{
    /// <summary>
    /// HediffComp that provides simple severity progression over time.
    /// 
    /// Unlike HediffComp_Immunizable, this does NOT hook into the vanilla disease widget
    /// or immunity system. It simply increments severity each tick based on severityPerDay.
    /// 
    /// This is ideal for standalone diseases that need progression without the full
    /// immunizable/severity tracking system that shows up in the vanilla health tab.
    /// </summary>
    public class HediffComp_SeverityPerDay : HediffComp
    {
        private HediffCompProperties_SeverityPerDay Props => (HediffCompProperties_SeverityPerDay)props;

        public override void CompPostTick()
        {
            base.CompPostTick();

            // Only progress if severityPerDay is positive (disease gets worse)
            if (Props.severityPerDay <= 0f)
            {
                return;
            }

            // Calculate severity increase for this tick
            // Severity per day / ticks per day (60000 ticks per day in RimWorld)
            float severityPerTick = Props.severityPerDay / 60000f;

            // Apply severity increase
            float newSeverity = parent.Severity + severityPerTick;

            // Cap at maxSeverity if specified
            if (Props.maxSeverity > 0f && newSeverity > Props.maxSeverity)
            {
                newSeverity = Props.maxSeverity;
            }

            parent.Severity = newSeverity;
        }

        public override string CompDebugString()
        {
            return $"severityPerDay: {Props.severityPerDay}\n" +
                   $"maxSeverity: {Props.maxSeverity}\n" +
                   $"currentSeverity: {parent.Severity:F3}";
        }
    }
}