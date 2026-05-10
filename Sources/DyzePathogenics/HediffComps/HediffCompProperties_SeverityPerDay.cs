using Verse;

namespace Dyze.RimWorld.Pathogenics
{
    /// <summary>
    /// Simple severity progression comp - increments severity over time without immunizable mechanics.
    /// Used to replace the vanilla HediffCompProperties_Immunizable for custom disease progression.
    /// </summary>
    public class HediffCompProperties_SeverityPerDay : HediffCompProperties
    {
        /// <summary>
        /// How much severity increases per day. Positive values mean the disease gets worse.
        /// </summary>
        public float severityPerDay = 0f;

        /// <summary>
        /// Optional maximum severity cap. Disease won't progress beyond this value.
        /// </summary>
        public float maxSeverity = 1f;

        public HediffCompProperties_SeverityPerDay()
        {
            compClass = typeof(HediffComp_SeverityPerDay);
        }
    }
}