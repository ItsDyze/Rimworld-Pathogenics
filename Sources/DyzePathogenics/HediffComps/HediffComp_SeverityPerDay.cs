using Verse;

namespace Dyze.RimWorld.Pathogenics
{
    /// <summary>
    /// HediffComp that provides simple severity progression over time.
    ///
    /// Unlike HediffComp_Immunizable, this does NOT hook into the vanilla disease widget
    /// or immunity system. It adds a fixed amount of severity each interval.
    /// When the configured max severity is reached, the disease removes itself.
    ///
    /// Release hardening note:
    /// - progression pauses while the Pathogenics master toggle is disabled
    /// - hidden state owns symptomatic/recovery transitions, so the comp no longer removes the hediff on its own
    /// </summary>
    public class HediffComp_SeverityPerDay : HediffComp
    {
        internal HediffCompProperties_SeverityPerDay Props => (HediffCompProperties_SeverityPerDay)props;

        public override void CompPostTick(ref float severityAdjustment)
        {
            base.CompPostTick(ref severityAdjustment);

            if (DyzePathogenicsMod.Settings?.Enabled != true)
            {
                return;
            }

            if (Props.severityPerDay <= 0f)
            {
                return;
            }

            float severityPerTick = Props.severityPerDay / 60000f;
            if (severityPerTick > 0f)
            {
                severityAdjustment += severityPerTick;
            }
        }

        public override string CompDebugString()
        {
            return $"severityPerDay: {Props.severityPerDay}\n" +
                   $"maxSeverity: {Props.maxSeverity}\n" +
                   $"currentSeverity: {parent.Severity:F3}\n" +
                   $"pathogenicsEnabled: {DyzePathogenicsMod.Settings?.Enabled}";
        }
    }
}
