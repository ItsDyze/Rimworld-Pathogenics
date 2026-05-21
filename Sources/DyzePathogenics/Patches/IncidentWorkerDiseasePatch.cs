using HarmonyLib;
using RimWorld;
using Verse;
using Dyze.RimWorld.Pathogenics.Integration;

namespace Dyze.RimWorld.Pathogenics.Patches
{
    /// <summary>
    /// Optional replacement layer for vanilla random disease incidents.
    ///
    /// Pathogenics can either suppress all vanilla disease incident workers, or only those whose
    /// disease has been explicitly integrated with the hidden Pathogenics simulation.
    /// </summary>
    [HarmonyPatch(typeof(IncidentWorker_Disease))]
    public static class IncidentWorkerDiseasePatch
    {
        [HarmonyPatch("CanFireNowSub")]
        [HarmonyPrefix]
        public static bool CanFireNowSubPrefix(IncidentWorker_Disease __instance, ref bool __result)
        {
            if (!ShouldSuppress(__instance))
            {
                return true;
            }

            __result = false;
            return false;
        }

        [HarmonyPatch("TryExecuteWorker")]
        [HarmonyPrefix]
        public static bool TryExecuteWorkerPrefix(IncidentWorker_Disease __instance, ref bool __result)
        {
            if (!ShouldSuppress(__instance))
            {
                return true;
            }

            __result = false;
            return false;
        }

        private static bool ShouldSuppress(IncidentWorker_Disease worker)
        {
            IncidentDef incidentDef = GetIncidentDef(worker);
            HediffDef diseaseDef = incidentDef?.diseaseIncident;
            DyzePathogenicsSettings settings = DyzePathogenicsMod.Settings;
            bool suppress = settings?.DisableAllVanillaDiseaseIncidents == true ||
                            PathogenicsDiseaseRegistry.ShouldSuppressVanillaIncident(diseaseDef);

            if (suppress && DyzePathogenicsMod.Settings?.EnableDebugLogging == true)
            {
                DyzeLog.Message($"Suppressed vanilla disease incident {incidentDef?.defName ?? "<unknown>"} for {diseaseDef?.defName ?? "<unknown>"}.");
            }

            return suppress;
        }

        private static IncidentDef GetIncidentDef(IncidentWorker_Disease worker)
        {
            return AccessTools.Field(typeof(IncidentWorker), "def")?.GetValue(worker) as IncidentDef;
        }
    }
}
