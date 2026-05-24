using RimWorld;
using Verse;

namespace Dyze.RimWorld.Pathogenics.Integration
{
    /// <summary>
    /// Data-driven description of a disease that can be handled by the Pathogenics hidden simulation.
    ///
    /// The hidden registry tracks disease state per pawn and per disease defName. The visible hediff,
    /// import eligibility, and incident suppression behavior are defined here so additional diseases can
    /// be registered without reworking the core loop.
    /// </summary>
    public class PathogenicsDiseaseProfile
    {
        public string HediffDefName { get; }
        public bool IsVanillaDisease { get; }
        public bool SupportsHiddenSimulation { get; }
        public bool CanImportFromOutsiders { get; }
        public bool UsesRespiratoryTransmission { get; }
        public bool SuppressVanillaIncidentWhenIntegratedSuppressionEnabled { get; }

        public PathogenicsDiseaseProfile(
            string hediffDefName,
            bool isVanillaDisease,
            bool supportsHiddenSimulation,
            bool canImportFromOutsiders,
            bool usesRespiratoryTransmission,
            bool suppressVanillaIncidentWhenIntegratedSuppressionEnabled)
        {
            HediffDefName = hediffDefName;
            IsVanillaDisease = isVanillaDisease;
            SupportsHiddenSimulation = supportsHiddenSimulation;
            CanImportFromOutsiders = canImportFromOutsiders;
            UsesRespiratoryTransmission = usesRespiratoryTransmission;
            SuppressVanillaIncidentWhenIntegratedSuppressionEnabled = suppressVanillaIncidentWhenIntegratedSuppressionEnabled;
        }

        public HediffDef HediffDef => DefDatabase<HediffDef>.GetNamedSilentFail(HediffDefName);

        public bool IsAvailable => HediffDef != null;
    }
}
