using System.Collections.Generic;
using System.Linq;
using System.Text;
using LudeonTK;
using RimWorld;
using Verse;
using Dyze.RimWorld.Pathogenics.Simulation;
using Dyze.RimWorld.Pathogenics.Integration;

namespace Dyze.RimWorld.Pathogenics
{
    public static class DyzeDebugActions
    {
        private const string CoronavirusDefName = PathogenicsDiseaseRegistry.DefaultDiseaseDefName;
        private const string VanillaFluDefName = PathogenicsDiseaseRegistry.IntegratedVanillaFluDefName;
        private const float DefaultSeverity = 0.15f;
        private const float DebugExposureAmount = 0.25f;
        private const float PulseExposureAmount = 0.5f;

        [DebugAction("Dyze Pathogenics", "Expose selected pawn to Coronavirus (hidden)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ExposePawnToCoronavirus(Pawn pawn) => SetHiddenExposure(pawn, CoronavirusDefName, 0f);

        [DebugAction("Dyze Pathogenics", "Expose selected pawn to Flu (hidden)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ExposePawnToVanillaFlu(Pawn pawn) => SetHiddenExposure(pawn, VanillaFluDefName, 0f);

        [DebugAction("Dyze Pathogenics", "Add Coronavirus exposure (0.25)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void AddCoronavirusExposureToPawn(Pawn pawn) => AddExposure(pawn, CoronavirusDefName, DebugExposureAmount);

        [DebugAction("Dyze Pathogenics", "Add Flu exposure (0.25)", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void AddVanillaFluExposureToPawn(Pawn pawn) => AddExposure(pawn, VanillaFluDefName, DebugExposureAmount);

        [DebugAction("Dyze Pathogenics", "Apply Coronavirus visible + hidden symptomatic", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ApplyCoronavirusToPawn(Pawn pawn) => ApplyVisibleDisease(pawn, CoronavirusDefName, DefaultSeverity);

        [DebugAction("Dyze Pathogenics", "Apply Flu visible + hidden symptomatic", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ApplyVanillaFluToPawn(Pawn pawn) => ApplyVisibleDisease(pawn, VanillaFluDefName, DefaultSeverity);

        [DebugAction("Dyze Pathogenics", "Remove Coronavirus only", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void RemoveCoronavirusFromPawn(Pawn pawn) => RemoveDisease(pawn, CoronavirusDefName);

        [DebugAction("Dyze Pathogenics", "Remove Flu only", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void RemoveVanillaFluFromPawn(Pawn pawn) => RemoveDisease(pawn, VanillaFluDefName);

        [DebugAction("Dyze Pathogenics", "Clear all hidden states for selected pawn", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ClearHiddenDiseaseStates(Pawn pawn)
        {
            if (!TryGetPawnAndMap(pawn, out PathogenicsMapComponent mapComponent)) return;
            int count = mapComponent.GetDiseaseStates(pawn).Count;
            mapComponent.ClearDiseaseState(pawn);
            Messages.Message($"Cleared {count} hidden disease state(s) for {pawn.LabelShort}.", MessageTypeDefOf.PositiveEvent, false);
        }

        [DebugAction("Dyze Pathogenics", "Log selected pawn disease details", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void LogPawnDiseaseState(Pawn pawn)
        {
            if (!TryGetPawnAndMap(pawn, out PathogenicsMapComponent mapComponent)) return;
            List<PawnDiseaseState> states = mapComponent.GetDiseaseStates(pawn).Where(state => state.HasDiseaseState()).ToList();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"=== Pathogenics states for {pawn.LabelShort} ===");
            sb.AppendLine($"Visible Pathogenics hediffs: {DescribeVisibleDiseaseDefs(pawn)}");
            if (states.Count == 0)
            {
                sb.AppendLine("No active hidden disease states.");
            }
            else
            {
                foreach (PawnDiseaseState state in states)
                {
                    sb.Append(state.GetDebugInfo(pawn));
                    sb.AppendLine($"Infectiousness: {InfectiousnessUtility.GetInfectiousness(pawn, state):F2}");
                    sb.AppendLine();
                }
            }

            DyzeLog.DevAction(sb.ToString());
            Messages.Message($"Logged {states.Count} disease state(s) for {pawn.LabelShort}.", MessageTypeDefOf.PositiveEvent, false);
        }

        [DebugAction("Dyze Pathogenics", "Show selected pawn disease details", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ShowPawnDiseaseDetails(Pawn pawn)
        {
            if (!TryGetPawnAndMap(pawn, out PathogenicsMapComponent mapComponent)) return;
            List<PawnDiseaseState> states = mapComponent.GetDiseaseStates(pawn).Where(state => state.HasDiseaseState()).ToList();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Visible Pathogenics hediffs: {DescribeVisibleDiseaseDefs(pawn)}");
            sb.AppendLine();
            if (states.Count == 0)
            {
                sb.AppendLine("No active hidden disease states.");
            }
            else
            {
                foreach (PawnDiseaseState state in states)
                {
                    sb.Append(state.GetDebugInfo(pawn));
                    sb.AppendLine($"Infectiousness: {InfectiousnessUtility.GetInfectiousness(pawn, state):F2}");
                    sb.AppendLine();
                }
            }

            Find.WindowStack.Add(new Dialog_MessageBox(sb.ToString(), $"Pathogenics: {pawn.LabelShort}"));
        }

        [DebugAction("Dyze Pathogenics", "Toggle Fast Transmission (10x)", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        public static void ToggleFastTransmission()
        {
            bool enabled = RespiratoryTransmissionWorker.DebugTransmissionMultiplier != 10f;
            RespiratoryTransmissionWorker.DebugTransmissionMultiplier = enabled ? 10f : 1f;
            Messages.Message($"Fast transmission {(enabled ? "ENABLED" : "DISABLED")} ({RespiratoryTransmissionWorker.DebugTransmissionMultiplier:F0}x).", enabled ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.NeutralEvent, false);
        }

        [DebugAction("Dyze Pathogenics", "Force transmission pulse from selected pawn", actionType = DebugActionType.ToolMapForPawns, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ForceTransmissionPulse(Pawn pawn)
        {
            if (!TryGetPawnAndMap(pawn, out PathogenicsMapComponent mapComponent)) return;
            List<PawnDiseaseState> infectiousStates = InfectiousnessUtility.GetInfectiousDiseaseStates(pawn)
                .Where(state => PathogenicsDiseaseRegistry.GetProfile(state)?.UsesRespiratoryTransmission == true)
                .ToList();
            if (infectiousStates.Count == 0)
            {
                Messages.Message($"{pawn.LabelShort} has no infectious respiratory disease states.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            int applications = 0;
            Dictionary<string, int> hitsByDisease = new Dictionary<string, int>();
            foreach (PawnDiseaseState sourceState in infectiousStates)
            {
                int targetsHit = 0;
                foreach (Pawn target in pawn.Map.mapPawns.AllPawnsSpawned)
                {
                    if (!IsValidPulseTarget(pawn, target, sourceState.DiseaseDefName)) continue;
                    float distance = pawn.Position.DistanceTo(target.Position);
                    if (distance > RespiratoryTransmissionWorker.MaxTransmissionRadius || distance < 0.1f) continue;
                    mapComponent.AddExposureToPawn(target, PulseExposureAmount, sourceState.DiseaseDefName);
                    targetsHit++;
                    applications++;
                }

                hitsByDisease[sourceState.DiseaseDefName] = targetsHit;
            }

            Messages.Message($"Transmission pulse from {pawn.LabelShort}: {applications} exposure applications ({string.Join(", ", hitsByDisease.Select(kvp => kvp.Key + "=" + kvp.Value).ToArray())}).", MessageTypeDefOf.PositiveEvent, false);
        }

        [DebugAction("Dyze Pathogenics", "Log mod status", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        public static void LogModStatus()
        {
            DyzePathogenicsSettings settings = DyzePathogenicsMod.Settings;
            string text =
                $"Disease Simulation: {settings?.Enabled}\n" +
                $"Affect Colonists Only: {settings?.AffectColonistsOnly}\n" +
                $"Debug Logging: {settings?.EnableDebugLogging}\n" +
                $"Outsider Importation: {settings?.EnableOutsiderImportation}\n" +
                $"Respiratory Spread: {settings?.EnableRespiratorySpread}\n" +
                $"Suppress Integrated Vanilla Incidents: {settings?.DisableIntegratedVanillaDiseaseIncidents}\n" +
                $"Suppress All Vanilla Incidents: {settings?.DisableAllVanillaDiseaseIncidents}\n" +
                $"Outsider Import Chance: {settings?.OutsiderImportChance:P0}\n" +
                $"Exposure Multiplier: {settings?.ExposureGainMultiplier:F1}x\n" +
                $"Show Debug Readout: {settings?.ShowDebugReadout}\n" +
                $"Integrated diseases: {string.Join(", ", PathogenicsDiseaseRegistry.AvailableProfiles().Select(profile => profile.HediffDefName).ToArray())}\n" +
                $"Tracked hidden disease states: {PathogenicsGameComponent.Instance?.PawnDiseaseStates.Count ?? 0}";
            Find.WindowStack.Add(new Dialog_MessageBox(text));
        }

        [DebugAction("Dyze Pathogenics", "Print simulation state", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        public static void PrintSimulationState()
        {
            Map map = Find.CurrentMap;
            PathogenicsMapComponent mapComponent = map?.GetComponent<PathogenicsMapComponent>();
            if (mapComponent == null)
            {
                Messages.Message("No map component found.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            List<KeyValuePair<Pawn, PawnDiseaseState>> activeStates = mapComponent.GetActiveDiseaseStates();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== Pathogenics Simulation State ===");
            sb.AppendLine($"Map ID: {map.uniqueID}");
            sb.AppendLine($"Active disease states on/near this map: {activeStates.Count}");
            foreach (KeyValuePair<Pawn, PawnDiseaseState> kvp in activeStates)
            {
                sb.AppendLine($"--- {kvp.Key.LabelShort} / {kvp.Value.DiseaseDefName} ---");
                sb.Append(kvp.Value.GetDebugInfo(kvp.Key));
            }

            DyzeLog.DevAction(sb.ToString());
            Messages.Message($"Simulation state logged ({activeStates.Count} active disease state(s)).", MessageTypeDefOf.PositiveEvent, false);
        }

        [DebugAction("Dyze Pathogenics", "Log registry health", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        public static void LogRegistryHealth()
        {
            PathogenicsGameComponent gameComponent = PathogenicsGameComponent.Instance;
            if (gameComponent == null)
            {
                Messages.Message("No Pathogenics game component found.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== Pathogenics Registry Health ===");
            sb.AppendLine($"Tracked hidden disease states: {gameComponent.PawnDiseaseStates.Count}");
            sb.AppendLine($"Distinct tracked pawns: {gameComponent.PawnDiseaseStates.Values.Select(state => state.PawnId).Distinct().Count()}");
            sb.AppendLine($"Checked outsider cache: {gameComponent.CheckedOutsiderPawnCount}");
            foreach (KeyValuePair<string, PawnDiseaseState> kvp in gameComponent.PawnDiseaseStates.OrderBy(kvp => kvp.Value.PawnId).ThenBy(kvp => kvp.Value.DiseaseDefName).Take(40))
            {
                Pawn trackedPawn = PathogenicsPawnLookup.FindAnyPawnById(kvp.Value.PawnId);
                string pawnLabel = trackedPawn?.LabelShort ?? $"ID {kvp.Value.PawnId}";
                sb.AppendLine($"- {kvp.Key}: {pawnLabel}, {kvp.Value.DiseaseDefName}, {kvp.Value.GetStageLabel()}, {PathogenicsPawnLookup.DescribePawnLocation(trackedPawn)}");
            }

            DyzeLog.DevAction(sb.ToString());
            Messages.Message("Registry health logged to console (F12).", MessageTypeDefOf.PositiveEvent, false);
        }

        [DebugAction("Dyze Pathogenics", "Reset all Pathogenics state worldwide", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        public static void ResetAllPathogenicsStateWorldwide()
        {
            PathogenicsGameComponent gameComponent = PathogenicsGameComponent.Instance;
            if (gameComponent == null)
            {
                Messages.Message("No Pathogenics game component found.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            int count = gameComponent.PawnDiseaseStates.Count;
            gameComponent.ClearAllDiseaseStates();
            Messages.Message($"Cleared {count} hidden disease state(s), matching visible hediffs, and outsider import cache worldwide.", MessageTypeDefOf.PositiveEvent, false);
        }

        private static void SetHiddenExposure(Pawn pawn, string diseaseDefName, float exposure)
        {
            if (!TryGetPawnAndMap(pawn, out PathogenicsMapComponent mapComponent)) return;
            if (!TryGetProfile(diseaseDefName, out PathogenicsDiseaseProfile profile)) return;

            int currentTick = Find.TickManager.TicksGame;
            PawnDiseaseState state = mapComponent.GetOrCreateDiseaseState(pawn, profile.HediffDefName);
            state.Stage = SimulatedDiseaseStage.Exposed;
            state.DiseaseDefName = profile.HediffDefName;
            state.ExposedTick = currentTick;
            state.InfectiousStartTick = -1;
            state.SymptomOnsetTick = -1;
            state.RecoveringTick = -1;
            state.RecoveredTick = -1;
            state.VisibleHediffApplied = false;
            state.Exposure = exposure;
            Messages.Message($"{pawn.LabelShort}: set hidden {DescribeDisease(profile)} exposure state (exposure={state.Exposure:F2}). Pawn now has {mapComponent.GetDiseaseStates(pawn).Count} tracked state(s).", MessageTypeDefOf.PositiveEvent, false);
        }

        private static void AddExposure(Pawn pawn, string diseaseDefName, float amount)
        {
            if (!TryGetPawnAndMap(pawn, out PathogenicsMapComponent mapComponent)) return;
            if (!TryGetProfile(diseaseDefName, out PathogenicsDiseaseProfile profile)) return;
            mapComponent.AddExposureToPawn(pawn, amount, profile.HediffDefName);
            PawnDiseaseState state = mapComponent.GetDiseaseState(pawn, profile.HediffDefName);
            Messages.Message($"{pawn.LabelShort}: added {amount:F2} {DescribeDisease(profile)} exposure; current same-disease exposure={state?.Exposure ?? 0f:F2}/1.00, stage={state?.GetStageLabel() ?? "<none>"}.", MessageTypeDefOf.PositiveEvent, false);
        }

        private static void ApplyVisibleDisease(Pawn pawn, string diseaseDefName, float severity)
        {
            if (!TryGetPawnAndMap(pawn, out PathogenicsMapComponent mapComponent)) return;
            if (!TryGetProfile(diseaseDefName, out PathogenicsDiseaseProfile profile)) return;

            Hediff existing = pawn.health?.hediffSet?.GetFirstHediffOfDef(profile.HediffDef);
            if (existing == null)
            {
                Hediff hediff = HediffMaker.MakeHediff(profile.HediffDef, pawn);
                hediff.Severity = severity;
                pawn.health.AddHediff(hediff);
            }
            else if (existing.Severity < severity)
            {
                existing.Severity = severity;
            }

            EnsureSymptomaticState(pawn, mapComponent, profile.HediffDefName);
            Messages.Message($"{pawn.LabelShort}: applied/refreshed visible {DescribeDisease(profile)} and matching hidden symptomatic state. Total tracked states={mapComponent.GetDiseaseStates(pawn).Count}.", MessageTypeDefOf.PositiveEvent, false);
        }

        private static void RemoveDisease(Pawn pawn, string diseaseDefName)
        {
            if (!TryGetPawnAndMap(pawn, out PathogenicsMapComponent mapComponent)) return;
            if (!TryGetProfile(diseaseDefName, out PathogenicsDiseaseProfile profile)) return;

            int visibleRemoved = 0;
            foreach (Hediff hediff in (pawn.health?.hediffSet?.hediffs ?? new List<Hediff>()).Where(hediff => hediff.def == profile.HediffDef).ToList())
            {
                pawn.health.RemoveHediff(hediff);
                visibleRemoved++;
            }

            bool hadHidden = mapComponent.GetDiseaseState(pawn, profile.HediffDefName) != null;
            mapComponent.ClearDiseaseState(pawn, profile.HediffDefName);
            Messages.Message($"{pawn.LabelShort}: removed {DescribeDisease(profile)} only (visible removed={visibleRemoved}, hidden removed={hadHidden}). Other disease states left intact: {mapComponent.GetDiseaseStates(pawn).Count}.", MessageTypeDefOf.PositiveEvent, false);
        }

        private static void EnsureSymptomaticState(Pawn pawn, PathogenicsMapComponent mapComponent, string diseaseDefName)
        {
            int currentTick = Find.TickManager.TicksGame;
            PawnDiseaseState state = mapComponent.GetOrCreateDiseaseState(pawn, diseaseDefName);
            if (state.ExposedTick < 0) state.ExposedTick = currentTick;
            if (state.InfectiousStartTick < 0) state.InfectiousStartTick = currentTick;
            state.Stage = SimulatedDiseaseStage.Symptomatic;
            state.SymptomOnsetTick = currentTick;
            if (state.RecoveringTick < 0) state.RecoveringTick = currentTick + 60000;
            state.VisibleHediffApplied = true;
            state.MapId = pawn.Map?.uniqueID ?? state.MapId;
            state.PreserveAcrossMaps = pawn.IsColonist;
        }

        private static bool TryGetPawnAndMap(Pawn pawn, out PathogenicsMapComponent mapComponent)
        {
            mapComponent = null;
            if (pawn == null)
            {
                Messages.Message("No pawn selected.", MessageTypeDefOf.RejectInput, false);
                return false;
            }

            mapComponent = pawn.Map?.GetComponent<PathogenicsMapComponent>() ?? Find.CurrentMap?.GetComponent<PathogenicsMapComponent>();
            if (mapComponent == null)
            {
                Messages.Message("Could not get Pathogenics map component.", MessageTypeDefOf.RejectInput, false);
                return false;
            }

            return true;
        }

        private static bool TryGetProfile(string diseaseDefName, out PathogenicsDiseaseProfile profile)
        {
            profile = PathogenicsDiseaseRegistry.GetProfile(diseaseDefName);
            if (profile == null || profile.HediffDef == null || !profile.SupportsHiddenSimulation || PathogenicsDiseaseRegistry.IsLegacyPathogenicFlu(profile.HediffDefName))
            {
                Messages.Message($"Disease '{diseaseDefName}' is not a usable Pathogenics hidden-simulation disease.", MessageTypeDefOf.RejectInput, false);
                return false;
            }

            return true;
        }

        private static bool IsValidPulseTarget(Pawn source, Pawn target, string diseaseDefName)
        {
            if (target == null || target == source || !target.Spawned || target.Dead || !target.RaceProps.Humanlike) return false;
            if (DyzePathogenicsMod.Settings?.AffectColonistsOnly == true && !target.IsColonist) return false;
            PawnDiseaseState targetState = PathogenicsGameComponent.Instance?.TryGetDiseaseState(target, diseaseDefName);
            return targetState == null || targetState.Stage < SimulatedDiseaseStage.Symptomatic;
        }

        private static string DescribeDisease(PathogenicsDiseaseProfile profile)
        {
            string defName = profile?.HediffDefName ?? "<unknown>";
            string label = profile?.HediffDef?.label?.CapitalizeFirst();
            return label.NullOrEmpty() ? defName : $"{label} ({defName})";
        }

        private static string DescribeVisibleDiseaseDefs(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null) return "none";
            List<string> visible = new List<string>();
            foreach (PathogenicsDiseaseProfile profile in PathogenicsDiseaseRegistry.AvailableProfiles())
            {
                if (profile.HediffDef != null && pawn.health.hediffSet.GetFirstHediffOfDef(profile.HediffDef) != null)
                {
                    visible.Add(profile.HediffDefName);
                }
            }

            return visible.Count == 0 ? "none" : string.Join(", ", visible.ToArray());
        }
    }
}
