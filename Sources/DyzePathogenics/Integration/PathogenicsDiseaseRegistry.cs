using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Dyze.RimWorld.Pathogenics.Integration
{
    /// <summary>
    /// Central registry for diseases that Pathogenics can drive through hidden state,
    /// outsider importation, respiratory spread, and vanilla incident suppression.
    /// </summary>
    public static class PathogenicsDiseaseRegistry
    {
        public const string DefaultDiseaseDefName = "DP_Coronavirus";
        public const string LegacyPathogenicFluDefName = "DP_PathogenicFlu";
        public const string IntegratedVanillaFluDefName = "Flu";

        private static readonly List<PathogenicsDiseaseProfile> Profiles = new List<PathogenicsDiseaseProfile>
        {
            new PathogenicsDiseaseProfile(
                DefaultDiseaseDefName,
                isVanillaDisease: false,
                supportsHiddenSimulation: true,
                canImportFromOutsiders: true,
                usesRespiratoryTransmission: true,
                suppressVanillaIncidentWhenIntegratedSuppressionEnabled: false),

            // Deprecated compatibility profile. Kept so old saves with DP_PathogenicFlu visible hediffs
            // or hidden states still load and can be inspected/resynchronized, but it is never imported,
            // transmitted, or selected by new debug/gameplay paths.
            new PathogenicsDiseaseProfile(
                LegacyPathogenicFluDefName,
                isVanillaDisease: false,
                supportsHiddenSimulation: true,
                canImportFromOutsiders: false,
                usesRespiratoryTransmission: false,
                suppressVanillaIncidentWhenIntegratedSuppressionEnabled: false),

            // Conservative first-pass vanilla integration. Flu is directly representable by the
            // current respiratory proximity model. Vector/environment diseases such as malaria
            // are intentionally excluded until matching transmission routes exist.
            new PathogenicsDiseaseProfile(
                IntegratedVanillaFluDefName,
                isVanillaDisease: true,
                supportsHiddenSimulation: true,
                canImportFromOutsiders: true,
                usesRespiratoryTransmission: true,
                suppressVanillaIncidentWhenIntegratedSuppressionEnabled: true)
        };

        public static IEnumerable<PathogenicsDiseaseProfile> AllProfiles => Profiles;

        public static PathogenicsDiseaseProfile DefaultProfile => GetProfile(DefaultDiseaseDefName) ?? AvailableProfiles().FirstOrDefault();

        public static IEnumerable<PathogenicsDiseaseProfile> AvailableProfiles()
        {
            return Profiles.Where(profile => profile.IsAvailable);
        }

        public static IEnumerable<PathogenicsDiseaseProfile> ImportableProfiles()
        {
            return Profiles.Where(profile => profile.IsAvailable && profile.SupportsHiddenSimulation && profile.CanImportFromOutsiders);
        }

        public static PathogenicsDiseaseProfile GetProfile(string hediffDefName)
        {
            if (hediffDefName.NullOrEmpty())
            {
                hediffDefName = DefaultDiseaseDefName;
            }

            return Profiles.FirstOrDefault(profile => profile.HediffDefName == hediffDefName);
        }

        public static PathogenicsDiseaseProfile GetProfile(HediffDef hediffDef)
        {
            return hediffDef == null ? null : GetProfile(hediffDef.defName);
        }

        public static PathogenicsDiseaseProfile GetProfile(PawnDiseaseState state)
        {
            return GetProfile(state?.DiseaseDefName) ?? DefaultProfile;
        }

        public static bool IsLegacyPathogenicFlu(string hediffDefName)
        {
            return hediffDefName == LegacyPathogenicFluDefName;
        }

        public static bool IsLegacyPathogenicFlu(PawnDiseaseState state)
        {
            return IsLegacyPathogenicFlu(state?.DiseaseDefName);
        }

        public static bool IsIntegratedVanillaDisease(HediffDef hediffDef)
        {
            PathogenicsDiseaseProfile profile = GetProfile(hediffDef);
            return profile != null && profile.IsVanillaDisease && profile.SupportsHiddenSimulation;
        }

        public static bool ShouldSuppressVanillaIncident(HediffDef diseaseDef)
        {
            if (diseaseDef == null)
            {
                return false;
            }

            DyzePathogenicsSettings settings = DyzePathogenicsMod.Settings;
            if (settings == null)
            {
                return false;
            }

            if (settings.DisableAllVanillaDiseaseIncidents)
            {
                return true;
            }

            if (!settings.DisableIntegratedVanillaDiseaseIncidents)
            {
                return false;
            }

            PathogenicsDiseaseProfile profile = GetProfile(diseaseDef);
            return profile != null &&
                   profile.IsVanillaDisease &&
                   profile.SupportsHiddenSimulation &&
                   profile.SuppressVanillaIncidentWhenIntegratedSuppressionEnabled;
        }
    }
}
