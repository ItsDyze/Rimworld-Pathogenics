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
        private const float DefaultCoronavirusSeverity = 0.15f;

        [DebugAction(
            "Dyze Pathogenics",
            "Expose selected pawn to Coronavirus (hidden infection)",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void ExposePawnToCoronavirus(Pawn pawn)
        {
            ExposePawnToDisease(pawn, CoronavirusDefName);
        }

        [DebugAction(
            "Dyze Pathogenics",
            "Expose selected pawn to Flu (hidden infection)",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void ExposePawnToVanillaFlu(Pawn pawn)
        {
            ExposePawnToDisease(pawn, VanillaFluDefName);
        }

        private static void ExposePawnToDisease(Pawn pawn, string diseaseDefName)
        {
            if (pawn == null)
            {
                Messages.Message(
                    "No pawn selected.",
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            PathogenicsMapComponent mapComponent = GetMapComponentForPawn(pawn);
            if (mapComponent == null)
            {
                Messages.Message(
                    "Could not get map component.",
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            PathogenicsDiseaseProfile profile = PathogenicsDiseaseRegistry.GetProfile(diseaseDefName);
            if (!IsUsableDiseaseProfile(profile, out string rejectReason))
            {
                Messages.Message(rejectReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            if (RejectConflictingActiveDiseaseState(pawn, mapComponent, profile, "exposing to"))
            {
                return;
            }

            int currentTick = Find.TickManager.TicksGame;
            PawnDiseaseState diseaseState = mapComponent.GetOrCreateDiseaseState(pawn, profile.HediffDefName);
            diseaseState.Stage = SimulatedDiseaseStage.Exposed;
            diseaseState.DiseaseDefName = profile.HediffDefName;
            diseaseState.ExposedTick = currentTick;
            diseaseState.InfectiousStartTick = -1;
            diseaseState.SymptomOnsetTick = -1;
            diseaseState.RecoveringTick = -1;
            diseaseState.RecoveredTick = -1;
            diseaseState.VisibleHediffApplied = false;
            diseaseState.ClearExposure();

            Messages.Message(
                $"{pawn.LabelShort} now has hidden exposed state for {DescribeDisease(profile)}.",
                MessageTypeDefOf.PositiveEvent,
                false
            );
        }

        [DebugAction(
            "Dyze Pathogenics",
            "Apply coronavirus to selected pawn",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void ApplyCoronavirusToPawn(Pawn pawn)
        {
            ApplyDiseaseToPawn(pawn, CoronavirusDefName, DefaultCoronavirusSeverity);
        }

        [DebugAction(
            "Dyze Pathogenics",
            "Apply Flu to selected pawn",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void ApplyVanillaFluToPawn(Pawn pawn)
        {
            ApplyDiseaseToPawn(pawn, VanillaFluDefName, DefaultCoronavirusSeverity);
        }

        private static void ApplyDiseaseToPawn(Pawn pawn, string diseaseDefName, float severity)
        {
            if (pawn == null)
            {
                Messages.Message(
                    "No pawn selected.",
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            PathogenicsDiseaseProfile profile = PathogenicsDiseaseRegistry.GetProfile(diseaseDefName);
            if (!IsUsableDiseaseProfile(profile, out string rejectReason))
            {
                Messages.Message(
                    rejectReason,
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            PathogenicsMapComponent mapComponent = GetMapComponentForPawn(pawn);
            if (RejectConflictingActiveDiseaseState(pawn, mapComponent, profile, "applying"))
            {
                return;
            }

            HediffDef diseaseDef = profile.HediffDef;
            Hediff existing = pawn.health?.hediffSet?.GetFirstHediffOfDef(diseaseDef);
            if (existing != null)
            {
                if (existing.Severity < severity)
                {
                    existing.Severity = severity;
                }

                EnsureSymptomaticDiseaseState(pawn, profile.HediffDefName);

                Messages.Message(
                    $"{pawn.LabelShort} already has {DescribeDisease(profile)}. Severity refreshed and hidden disease state synchronized for {profile.HediffDefName}.",
                    MessageTypeDefOf.NeutralEvent,
                    false
                );
                return;
            }
            Hediff hediff = HediffMaker.MakeHediff(diseaseDef, pawn);
            hediff.Severity = severity;
            pawn.health.AddHediff(hediff);

            EnsureSymptomaticDiseaseState(pawn, profile.HediffDefName);

            Messages.Message(
                $"Applied {DescribeDisease(profile)} to {pawn.LabelShort} and synchronized hidden disease state for {profile.HediffDefName}.",
                MessageTypeDefOf.PositiveEvent,
                false
            );
        }

        [DebugAction(
            "Dyze Pathogenics",
            "Remove coronavirus from selected pawn",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void RemoveCoronavirusFromPawn(Pawn pawn)
        {
            RemoveDiseaseFromPawn(pawn, CoronavirusDefName);
        }

        [DebugAction(
            "Dyze Pathogenics",
            "Remove Flu from selected pawn",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void RemoveVanillaFluFromPawn(Pawn pawn)
        {
            RemoveDiseaseFromPawn(pawn, VanillaFluDefName);
        }

        private static void RemoveDiseaseFromPawn(Pawn pawn, string diseaseDefName)
        {
            if (pawn == null)
            {
                Messages.Message(
                    "No pawn selected.",
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            PathogenicsDiseaseProfile profile = PathogenicsDiseaseRegistry.GetProfile(diseaseDefName);
            if (profile?.HediffDef == null)
            {
                Messages.Message(
                    $"Could not find HediffDef '{diseaseDefName}'.",
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            var hediffs = pawn.health?.hediffSet?.hediffs;
            if (hediffs == null)
            {
                Messages.Message(
                    $"{pawn.LabelShort} has no health conditions to inspect.",
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            int removedCount = 0;
            foreach (Hediff hediff in hediffs.Where(hediff => hediff.def == profile.HediffDef).ToList())
            {
                pawn.health.RemoveHediff(hediff);
                removedCount++;
            }

            if (removedCount <= 0)
            {
                Messages.Message(
                    $"{pawn.LabelShort} does not have {DescribeDisease(profile)}.",
                    MessageTypeDefOf.NeutralEvent,
                    false
                );
                return;
            }

            PathogenicsMapComponent mapComponent = GetMapComponentForPawn(pawn);
            PawnDiseaseState state = mapComponent?.GetDiseaseState(pawn);
            bool clearedHiddenState = state != null && state.DiseaseDefName == profile.HediffDefName;
            if (clearedHiddenState)
            {
                mapComponent.ClearDiseaseState(pawn);
            }

            Messages.Message(
                $"Removed {DescribeDisease(profile)} from {pawn.LabelShort}" +
                (clearedHiddenState ? $" and cleared matching hidden disease state ({profile.HediffDefName})." : "; hidden state was for another disease and was left intact."),
                MessageTypeDefOf.PositiveEvent,
                false
            );
        }

        [DebugAction(
            "Dyze Pathogenics",
            "Add exposure (0.25) to selected pawn",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void AddExposureToPawn(Pawn pawn)
        {
            AddExposureToPawn(pawn, CoronavirusDefName);
        }

        [DebugAction(
            "Dyze Pathogenics",
            "Add Flu exposure (0.25) to selected pawn",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void AddVanillaFluExposureToPawn(Pawn pawn)
        {
            AddExposureToPawn(pawn, VanillaFluDefName);
        }

        private static void AddExposureToPawn(Pawn pawn, string diseaseDefName)
        {
            if (pawn == null)
            {
                Messages.Message(
                    "No pawn selected.",
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            PathogenicsMapComponent mapComponent = GetMapComponentForPawn(pawn);
            if (mapComponent == null)
            {
                Messages.Message(
                    "Could not get map component.",
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            PathogenicsDiseaseProfile profile = PathogenicsDiseaseRegistry.GetProfile(diseaseDefName);
            if (!IsUsableDiseaseProfile(profile, out string rejectReason))
            {
                Messages.Message(rejectReason, MessageTypeDefOf.RejectInput, false);
                return;
            }

            PawnDiseaseState existingState = mapComponent.GetDiseaseState(pawn);
            if (existingState != null && existingState.HasDiseaseState() && existingState.DiseaseDefName != profile.HediffDefName)
            {
                Messages.Message(
                    $"{pawn.LabelShort} already has active {DescribeDisease(existingState)}; not adding {DescribeDisease(profile)} exposure because only one active Pathogenics disease state is tracked per pawn.",
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            mapComponent.AddExposureToPawn(pawn, 0.25f, profile.HediffDefName);

            PawnDiseaseState state = mapComponent.GetDiseaseState(pawn);
            float currentExposure = state?.Exposure ?? 0f;

            Messages.Message(
                $"{pawn.LabelShort} gained {DescribeDisease(profile)} exposure. Current {state?.DiseaseDefName ?? profile.HediffDefName}: {currentExposure:F2} / 1.00",
                MessageTypeDefOf.PositiveEvent,
                false
            );
        }

        [DebugAction(
            "Dyze Pathogenics",
            "Clear exposure for selected pawn",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void ClearExposureForPawn(Pawn pawn)
        {
            if (pawn == null)
            {
                Messages.Message(
                    "No pawn selected.",
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            PathogenicsMapComponent mapComponent = GetMapComponentForPawn(pawn);
            if (mapComponent == null)
            {
                Messages.Message(
                    "Could not get map component.",
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            PawnDiseaseState state = mapComponent.GetDiseaseState(pawn);
            if (state == null)
            {
                Messages.Message(
                    $"{pawn.LabelShort} has no exposure to clear.",
                    MessageTypeDefOf.NeutralEvent,
                    false
                );
                return;
            }

            string diseaseDescription = DescribeDisease(state);
            state.ClearExposure();
            if (state.Stage == SimulatedDiseaseStage.Exposed)
            {
                mapComponent.ClearDiseaseState(pawn);
            }

            Messages.Message(
                $"Cleared exposure for {pawn.LabelShort} ({diseaseDescription}).",
                MessageTypeDefOf.PositiveEvent,
                false
            );
        }

        [DebugAction(
            "Dyze Pathogenics",
            "Clear hidden disease state for selected pawn",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void ClearHiddenDiseaseState(Pawn pawn)
        {
            if (pawn == null)
            {
                Messages.Message(
                    "No pawn selected.",
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            PathogenicsMapComponent mapComponent = GetMapComponentForPawn(pawn);
            if (mapComponent == null)
            {
                Messages.Message(
                    "Could not get map component.",
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            PawnDiseaseState state = mapComponent.GetDiseaseState(pawn);
            string diseaseDescription = state != null ? DescribeDisease(state) : "no tracked disease";
            mapComponent.ClearDiseaseState(pawn);

            Messages.Message(
                $"Cleared hidden disease state for {pawn.LabelShort} ({diseaseDescription}).",
                MessageTypeDefOf.PositiveEvent,
                false
            );
        }

        [DebugAction(
            "Dyze Pathogenics",
            "Log disease state for selected pawn",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void LogPawnDiseaseState(Pawn pawn)
        {
            if (pawn == null)
            {
                DyzeLog.DevAction("No pawn selected.");
                return;
            }

            PathogenicsMapComponent mapComponent = GetMapComponentForPawn(pawn);
            if (mapComponent == null)
            {
                DyzeLog.DevAction($"Pawn {pawn.LabelShort} has no map component.");
                return;
            }

            PawnDiseaseState diseaseState = mapComponent.GetDiseaseState(pawn);
            string visibleDiseaseDefName = GetVisiblePathogenicsDiseaseDefName(pawn);
            if (diseaseState == null && !visibleDiseaseDefName.NullOrEmpty())
            {
                diseaseState = EnsureSymptomaticDiseaseState(pawn, visibleDiseaseDefName);
            }

            if (diseaseState == null || !diseaseState.HasDiseaseState())
            {
                DyzeLog.DevAction($"Pawn {pawn.LabelShort} has no active disease state.");
                return;
            }

            DyzeLog.DevAction(diseaseState.GetDebugInfo(pawn));
        }

        [DebugAction(
            "Dyze Pathogenics",
            "Toggle Fast Transmission (10x)",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing
        )]
        public static void ToggleFastTransmission()
        {
            bool newValue = RespiratoryTransmissionWorker.DebugTransmissionMultiplier != 10f;
            RespiratoryTransmissionWorker.DebugTransmissionMultiplier = newValue ? 10f : 1f;

            Messages.Message(
                $"Fast transmission {(newValue ? "ENABLED" : "DISABLED")} (10x multiplier)",
                newValue ? MessageTypeDefOf.PositiveEvent : MessageTypeDefOf.NeutralEvent,
                false
            );
        }

        [DebugAction(
            "Dyze Pathogenics",
            "Force transmission pulse",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void ForceTransmissionPulse(Pawn pawn)
        {
            if (pawn == null)
            {
                Messages.Message(
                    "No pawn selected.",
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            PathogenicsMapComponent mapComponent = GetMapComponentForPawn(pawn);
            if (mapComponent == null)
            {
                Messages.Message(
                    "Could not get map component.",
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            float infectiousness = InfectiousnessUtility.GetInfectiousness(pawn);
            if (infectiousness <= 0f)
            {
                PawnDiseaseState nonInfectiousState = PathogenicsGameComponent.Instance?.TryGetDiseaseState(pawn);
                Messages.Message(
                    $"{pawn.LabelShort} is not infectious for {DescribeDisease(nonInfectiousState)} (no disease state or not contagious yet).",
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            const float PulseExposureAmount = 0.5f;
            int targetsHit = 0;

            PawnDiseaseState sourceState = PathogenicsGameComponent.Instance?.TryGetDiseaseState(pawn);
            string sourceDiseaseDefName = sourceState?.DiseaseDefName;
            var allPawns = pawn.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn target = allPawns[i];
                if (target == null || target == pawn || !target.Spawned || target.Dead)
                {
                    continue;
                }

                if (!target.RaceProps.Humanlike)
                {
                    continue;
                }

                float distance = pawn.Position.DistanceTo(target.Position);
                if (distance > RespiratoryTransmissionWorker.MaxTransmissionRadius || distance < 0.1f)
                {
                    continue;
                }

                PawnDiseaseState targetState = PathogenicsGameComponent.Instance?.TryGetDiseaseState(target);
                if (targetState != null && targetState.Stage >= SimulatedDiseaseStage.Symptomatic)
                {
                    continue;
                }

                mapComponent.AddExposureToPawn(target, PulseExposureAmount, sourceDiseaseDefName);
                targetsHit++;
            }

            Messages.Message(
                $"Transmission pulse from {pawn.LabelShort} ({DescribeDisease(sourceState)}): hit {targetsHit} targets (+{PulseExposureAmount:F2} exposure each)",
                MessageTypeDefOf.PositiveEvent,
                false
            );
        }

        [DebugAction(
            "Dyze Pathogenics",
            "Log mod status",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing
        )]
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
                $"Integrated diseases: {string.Join(", ", PathogenicsDiseaseRegistry.AvailableProfiles().Select(profile => profile.HediffDefName).ToArray())}\n\n" +
                "Release-hardened build: global disease registry + pause-safe toggle + vanilla disease integration.";

            Find.WindowStack.Add(new Dialog_MessageBox(text));
        }

        [DebugAction(
            "Dyze Pathogenics",
            "Print simulation state",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing
        )]
        public static void PrintSimulationState()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                Messages.Message("No map loaded.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            PathogenicsMapComponent mapComponent = map.GetComponent<PathogenicsMapComponent>();
            if (mapComponent == null)
            {
                Messages.Message("No map component found.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            var activeStates = mapComponent.GetActiveDiseaseStates();
            if (activeStates.Count == 0)
            {
                Messages.Message("No pawns with active disease state on this map.", MessageTypeDefOf.NeutralEvent, false);
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== Pathogenics Simulation State ===");
            sb.AppendLine($"Map ID: {map.uniqueID}");
            sb.AppendLine($"Pawns with active disease state: {activeStates.Count}");
            sb.AppendLine();

            foreach (var kvp in activeStates)
            {
                Pawn pawn = kvp.Key;
                PawnDiseaseState state = kvp.Value;
                sb.AppendLine($"--- {pawn.LabelShort} ---");
                sb.Append(state.GetDebugInfo(pawn));
                sb.AppendLine();
            }

            DyzeLog.DevAction(sb.ToString());
            Messages.Message("Simulation state logged to console (F12).", MessageTypeDefOf.PositiveEvent, false);
        }

        [DebugAction(
            "Dyze Pathogenics",
            "Show pawn disease details",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void ShowPawnDiseaseDetails(Pawn pawn)
        {
            if (pawn == null)
            {
                Messages.Message("No pawn selected.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            PathogenicsMapComponent mapComponent = GetMapComponentForPawn(pawn);
            if (mapComponent == null)
            {
                Messages.Message("Could not get map component.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            PawnDiseaseState diseaseState = mapComponent.GetDiseaseState(pawn);
            if (diseaseState == null || !diseaseState.HasDiseaseState())
            {
                Messages.Message(
                    $"{pawn.LabelShort} has no active disease state.",
                    MessageTypeDefOf.NeutralEvent,
                    false
                );
                return;
            }

            string title = $"Disease State: {pawn.LabelShort}";
            string content = diseaseState.GetDebugInfo(pawn);
            float infectiousness = InfectiousnessUtility.GetInfectiousnessForPawn(pawn);
            content += $"\nInfectiousness: {infectiousness:F2}";

            if (diseaseState.Stage == SimulatedDiseaseStage.Exposed)
            {
                content += $"\nExposure: {diseaseState.Exposure:F2} / 1.00";
            }

            Find.WindowStack.Add(new Dialog_MessageBox(content, title));
        }

        [DebugAction(
            "Dyze Pathogenics",
            "Log registry health",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing
        )]
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
            sb.AppendLine($"Tracked disease states: {gameComponent.PawnDiseaseStates.Count}");
            sb.AppendLine($"Checked outsider cache: {gameComponent.CheckedOutsiderPawnCount}");
            sb.AppendLine($"Simulation enabled: {DyzePathogenicsMod.Settings?.Enabled}");

            int shown = 0;
            foreach (var kvp in gameComponent.PawnDiseaseStates.OrderBy(kvp => kvp.Key))
            {
                Pawn trackedPawn = PathogenicsPawnLookup.FindAnyPawnById(kvp.Key);
                string pawnLabel = trackedPawn?.LabelShort ?? $"ID {kvp.Key}";
                string location = PathogenicsPawnLookup.DescribePawnLocation(trackedPawn);
                string disease = kvp.Value?.DiseaseDefName ?? "<null>";
                sb.AppendLine($"- {pawnLabel}: {disease}, {kvp.Value?.GetStageLabel() ?? "<null>"}, {location}");
                shown++;
                if (shown >= 20)
                {
                    sb.AppendLine("- …");
                    break;
                }
            }

            DyzeLog.DevAction(sb.ToString());
            Messages.Message("Registry health logged to console (F12).", MessageTypeDefOf.PositiveEvent, false);
        }

        [DebugAction(
            "Dyze Pathogenics",
            "Reset all Pathogenics state worldwide",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.Playing
        )]
        public static void ResetAllPathogenicsStateWorldwide()
        {
            PathogenicsGameComponent gameComponent = PathogenicsGameComponent.Instance;
            if (gameComponent == null)
            {
                Messages.Message("No Pathogenics game component found.", MessageTypeDefOf.RejectInput, false);
                return;
            }

            gameComponent.ClearAllDiseaseStates();
            Messages.Message("Cleared all Pathogenics hidden state, visible hediffs, and outsider import cache worldwide.", MessageTypeDefOf.PositiveEvent, false);
        }

        [DebugAction(
            "Dyze Pathogenics",
            "Log selected pawn cross-map state",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void LogPawnCrossMapState(Pawn pawn)
        {
            if (pawn == null)
            {
                DyzeLog.DevAction("No pawn selected.");
                return;
            }

            PawnDiseaseState diseaseState = PathogenicsGameComponent.Instance?.TryGetDiseaseState(pawn);
            string mapInfo = pawn.Map != null ? $"map {pawn.Map.uniqueID}" : "off-map/caravan";
            string visibleDiseaseDefName = GetVisiblePathogenicsDiseaseDefName(pawn);
            string visible = visibleDiseaseDefName.NullOrEmpty() ? "no" : visibleDiseaseDefName;

            if (diseaseState == null)
            {
                DyzeLog.DevAction($"Pawn {pawn.LabelShort}: no registry state, location={mapInfo}, visibleHediff={visible}");
                return;
            }

            DyzeLog.DevAction($"Pawn {pawn.LabelShort}: location={mapInfo}, disease={DescribeDisease(diseaseState)}, stateMapId={diseaseState.MapId}, stage={diseaseState.GetStageLabel()}, exposure={diseaseState.Exposure:F2}, visibleHediff={visible}, preserveAcrossMaps={diseaseState.PreserveAcrossMaps}");
        }

        private static bool IsUsableDiseaseProfile(PathogenicsDiseaseProfile profile, out string rejectReason)
        {
            if (profile == null)
            {
                rejectReason = "Unknown Pathogenics disease profile.";
                return false;
            }

            if (PathogenicsDiseaseRegistry.IsLegacyPathogenicFlu(profile.HediffDefName))
            {
                rejectReason = $"'{profile.HediffDefName}' is a deprecated compatibility disease and cannot be used by debug actions.";
                return false;
            }

            if (!profile.SupportsHiddenSimulation)
            {
                rejectReason = $"'{profile.HediffDefName}' does not support hidden Pathogenics simulation.";
                return false;
            }

            if (profile.HediffDef == null)
            {
                rejectReason = $"Could not find HediffDef '{profile.HediffDefName}'.";
                return false;
            }

            rejectReason = null;
            return true;
        }

        private static bool RejectConflictingActiveDiseaseState(
            Pawn pawn,
            PathogenicsMapComponent mapComponent,
            PathogenicsDiseaseProfile profile,
            string actionDescription
        )
        {
            PawnDiseaseState existingState = mapComponent?.GetDiseaseState(pawn);
            if (existingState == null || !existingState.HasDiseaseState() || existingState.DiseaseDefName == profile.HediffDefName)
            {
                return false;
            }

            Messages.Message(
                $"{pawn.LabelShort} already has active {DescribeDisease(existingState)}; not {actionDescription} {DescribeDisease(profile)} because only one active Pathogenics disease state is tracked per pawn.",
                MessageTypeDefOf.RejectInput,
                false
            );
            return true;
        }

        private static string DescribeDisease(PawnDiseaseState state)
        {
            if (state == null)
            {
                return "no tracked disease";
            }

            return DescribeDisease(PathogenicsDiseaseRegistry.GetProfile(state), state.DiseaseDefName);
        }

        private static string DescribeDisease(PathogenicsDiseaseProfile profile, string fallbackDefName = null)
        {
            string defName = profile?.HediffDefName ?? fallbackDefName ?? "<unknown>";
            string label = profile?.HediffDef?.label?.CapitalizeFirst();
            return label.NullOrEmpty() ? defName : $"{label} ({defName})";
        }

        private static PathogenicsMapComponent GetMapComponentForPawn(Pawn pawn)
        {
            if (pawn?.Map != null)
            {
                return pawn.Map.GetComponent<PathogenicsMapComponent>();
            }

            return Find.CurrentMap?.GetComponent<PathogenicsMapComponent>();
        }

        private static PawnDiseaseState EnsureSymptomaticDiseaseState(Pawn pawn, string diseaseDefName = PathogenicsDiseaseRegistry.DefaultDiseaseDefName)
        {
            if (PathogenicsDiseaseRegistry.IsLegacyPathogenicFlu(diseaseDefName))
            {
                return null;
            }

            PathogenicsMapComponent mapComponent = GetMapComponentForPawn(pawn);
            if (mapComponent == null)
            {
                return null;
            }

            int currentTick = Find.TickManager.TicksGame;
            PawnDiseaseState diseaseState = mapComponent.GetOrCreateDiseaseState(pawn, diseaseDefName);
            if (diseaseState == null)
            {
                return null;
            }

            if (diseaseState.ExposedTick < 0)
            {
                diseaseState.ExposedTick = currentTick;
            }

            if (diseaseState.InfectiousStartTick < 0)
            {
                diseaseState.InfectiousStartTick = currentTick;
            }

            diseaseState.Stage = SimulatedDiseaseStage.Symptomatic;
            diseaseState.SymptomOnsetTick = currentTick;
            diseaseState.VisibleHediffApplied = true;
            diseaseState.MapId = pawn.Map?.uniqueID ?? diseaseState.MapId;
            diseaseState.PreserveAcrossMaps = pawn.IsColonist;
            return diseaseState;
        }

        private static string GetVisiblePathogenicsDiseaseDefName(Pawn pawn)
        {
            if (pawn?.health?.hediffSet == null)
            {
                return null;
            }

            foreach (PathogenicsDiseaseProfile profile in PathogenicsDiseaseRegistry.AvailableProfiles())
            {
                HediffDef hediffDef = profile.HediffDef;
                if (hediffDef != null && pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef) != null)
                {
                    return hediffDef.defName;
                }
            }

            return null;
        }
    }
}
