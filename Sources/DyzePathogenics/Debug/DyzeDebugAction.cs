using System.Linq;
using System.Text;
using LudeonTK;
using RimWorld;
using Verse;
using Dyze.RimWorld.Pathogenics.Simulation;

namespace Dyze.RimWorld.Pathogenics
{
    public static class DyzeDebugActions
    {
        private const string PathogenicFluDefName = "DP_PathogenicFlu";
        private const float DefaultPathogenicFluSeverity = 0.15f;

        [DebugAction(
            "Dyze Pathogenics",
            "Expose selected pawn (hidden infection)",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void ExposePawnToPathogenicFlu(Pawn pawn)
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

            int currentTick = Find.TickManager.TicksGame;
            PawnDiseaseState diseaseState = mapComponent.GetOrCreateDiseaseState(pawn);
            diseaseState.Stage = SimulatedDiseaseStage.Exposed;
            diseaseState.ExposedTick = currentTick;
            diseaseState.InfectiousStartTick = -1;
            diseaseState.SymptomOnsetTick = -1;
            diseaseState.RecoveringTick = -1;
            diseaseState.RecoveredTick = -1;
            diseaseState.ClearExposure();

            Messages.Message(
                $"{pawn.LabelShort} now has a hidden exposed state.",
                MessageTypeDefOf.PositiveEvent,
                false
            );
        }

        [DebugAction(
            "Dyze Pathogenics",
            "Apply pathogenic flu to selected pawn",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void ApplyPathogenicFluToPawn(Pawn pawn)
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

            HediffDef pathogenicFlu = GetPathogenicFluDef();
            if (pathogenicFlu == null)
            {
                Messages.Message(
                    $"Could not find HediffDef '{PathogenicFluDefName}'.",
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            Hediff existing = pawn.health?.hediffSet?.GetFirstHediffOfDef(pathogenicFlu);
            if (existing != null)
            {
                if (existing.Severity < DefaultPathogenicFluSeverity)
                {
                    existing.Severity = DefaultPathogenicFluSeverity;
                }

                EnsureSymptomaticDiseaseState(pawn);

                Messages.Message(
                    $"{pawn.LabelShort} already has {pathogenicFlu.label}. Severity refreshed and disease state synchronized.",
                    MessageTypeDefOf.NeutralEvent,
                    false
                );
                return;
            }

            Hediff hediff = HediffMaker.MakeHediff(pathogenicFlu, pawn);
            hediff.Severity = DefaultPathogenicFluSeverity;
            pawn.health.AddHediff(hediff);

            EnsureSymptomaticDiseaseState(pawn);

            Messages.Message(
                $"Applied {pathogenicFlu.label} to {pawn.LabelShort} and synchronized hidden disease state.",
                MessageTypeDefOf.PositiveEvent,
                false
            );
        }

        [DebugAction(
            "Dyze Pathogenics",
            "Remove pathogenic flu from selected pawn",
            actionType = DebugActionType.ToolMapForPawns,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void RemovePathogenicFluFromPawn(Pawn pawn)
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

            HediffDef pathogenicFlu = GetPathogenicFluDef();
            if (pathogenicFlu == null)
            {
                Messages.Message(
                    $"Could not find HediffDef '{PathogenicFluDefName}'.",
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
            foreach (Hediff hediff in hediffs.Where(hediff => hediff.def == pathogenicFlu).ToList())
            {
                pawn.health.RemoveHediff(hediff);
                removedCount++;
            }

            if (removedCount <= 0)
            {
                Messages.Message(
                    $"{pawn.LabelShort} does not have {pathogenicFlu.label}.",
                    MessageTypeDefOf.NeutralEvent,
                    false
                );
                return;
            }

            PathogenicsMapComponent mapComponent = GetMapComponentForPawn(pawn);
            mapComponent?.ClearDiseaseState(pawn);

            Messages.Message(
                $"Removed {pathogenicFlu.label} from {pawn.LabelShort} and cleared hidden disease state.",
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

            mapComponent.AddExposureToPawn(pawn, 0.25f);

            PawnDiseaseState state = mapComponent.GetDiseaseState(pawn);
            float currentExposure = state?.Exposure ?? 0f;

            Messages.Message(
                $"{pawn.LabelShort} gained exposure. Current: {currentExposure:F2} / 1.00",
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

            state.ClearExposure();
            if (state.Stage == SimulatedDiseaseStage.Exposed)
            {
                mapComponent.ClearDiseaseState(pawn);
            }

            Messages.Message(
                $"Cleared exposure for {pawn.LabelShort}.",
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

            mapComponent.ClearDiseaseState(pawn);

            Messages.Message(
                $"Cleared hidden disease state for {pawn.LabelShort}.",
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
            if (diseaseState == null && PawnHasVisiblePathogenicFlu(pawn))
            {
                diseaseState = EnsureSymptomaticDiseaseState(pawn);
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
                Messages.Message(
                    $"{pawn.LabelShort} is not infectious (no disease state or not contagious yet).",
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            const float PulseExposureAmount = 0.5f;
            int targetsHit = 0;

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

                mapComponent.AddExposureToPawn(target, PulseExposureAmount);
                targetsHit++;
            }

            Messages.Message(
                $"Transmission pulse from {pawn.LabelShort}: hit {targetsHit} targets (+{PulseExposureAmount:F2} exposure each)",
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
                $"Outsider Import Chance: {settings?.OutsiderImportChance:P0}\n" +
                $"Exposure Multiplier: {settings?.ExposureGainMultiplier:F1}x\n" +
                $"Show Debug Readout: {settings?.ShowDebugReadout}\n\n" +
                "Release-hardened build: global disease registry + pause-safe toggle.";

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
                sb.AppendLine($"- {pawnLabel}: {kvp.Value?.GetStageLabel() ?? "<null>"}, {location}");
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
            "Reset Pathogenics state on current map",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.PlayingOnMap
        )]
        public static void ResetPathogenicsStateOnCurrentMap()
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

            mapComponent.ClearAllDiseaseStates();
            Messages.Message("Cleared Pathogenics hidden state and visible hediffs for the current map.", MessageTypeDefOf.PositiveEvent, false);
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
            string visible = PawnHasVisiblePathogenicFlu(pawn) ? "yes" : "no";

            if (diseaseState == null)
            {
                DyzeLog.DevAction($"Pawn {pawn.LabelShort}: no registry state, location={mapInfo}, visibleHediff={visible}");
                return;
            }

            DyzeLog.DevAction($"Pawn {pawn.LabelShort}: location={mapInfo}, stateMapId={diseaseState.MapId}, stage={diseaseState.GetStageLabel()}, visibleHediff={visible}, preserveAcrossMaps={diseaseState.PreserveAcrossMaps}");
        }

        private static PathogenicsMapComponent GetMapComponentForPawn(Pawn pawn)
        {
            if (pawn?.Map != null)
            {
                return pawn.Map.GetComponent<PathogenicsMapComponent>();
            }

            return Find.CurrentMap?.GetComponent<PathogenicsMapComponent>();
        }

        private static PawnDiseaseState EnsureSymptomaticDiseaseState(Pawn pawn)
        {
            PathogenicsMapComponent mapComponent = GetMapComponentForPawn(pawn);
            if (mapComponent == null)
            {
                return null;
            }

            int currentTick = Find.TickManager.TicksGame;
            PawnDiseaseState diseaseState = mapComponent.GetOrCreateDiseaseState(pawn);
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

        private static bool PawnHasVisiblePathogenicFlu(Pawn pawn)
        {
            HediffDef pathogenicFlu = GetPathogenicFluDef();
            return pathogenicFlu != null && pawn.health?.hediffSet?.GetFirstHediffOfDef(pathogenicFlu) != null;
        }

        private static HediffDef GetPathogenicFluDef()
        {
            return DefDatabase<HediffDef>.GetNamedSilentFail(PathogenicFluDefName);
        }
    }
}
