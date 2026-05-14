using System.Linq;
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

            var mapComponent = pawn.Map?.GetComponent<PathogenicsMapComponent>();
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

            var mapComponent = pawn.Map?.GetComponent<PathogenicsMapComponent>();
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

            var mapComponent = pawn.Map?.GetComponent<PathogenicsMapComponent>();
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

            var state = mapComponent.GetDiseaseState(pawn);
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

            var mapComponent = pawn.Map?.GetComponent<PathogenicsMapComponent>();
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

            var mapComponent = pawn.Map?.GetComponent<PathogenicsMapComponent>();
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

            var mapComponent = pawn.Map?.GetComponent<PathogenicsMapComponent>();
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

        /// <summary>
        /// v0.2.2: Toggle fast transmission mode (10x) for accelerated testing.
        /// </summary>
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

        /// <summary>
        /// v0.2.2: Force a transmission pulse from selected pawn to all nearby targets.
        /// Useful for testing spread without waiting for natural accumulation.
        /// </summary>
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

            var mapComponent = pawn.Map?.GetComponent<PathogenicsMapComponent>();
            if (mapComponent == null)
            {
                Messages.Message(
                    "Could not get map component.",
                    MessageTypeDefOf.RejectInput,
                    false
                );
                return;
            }

            // Check if pawn is infectious
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

            // Force transmission pulse with high exposure
            const float PulseExposureAmount = 0.5f;
            int targetsHit = 0;

            var allPawns = pawn.Map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < allPawns.Count; i++)
            {
                Pawn target = allPawns[i];
                if (target == null || target == pawn || !target.Spawned || target.Dead)
                    continue;

                if (!target.RaceProps.Humanlike)
                    continue;

                // Check distance - use the max radius
                float distance = pawn.Position.DistanceTo(target.Position);
                if (distance > RespiratoryTransmissionWorker.MaxTransmissionRadius || distance < 0.1f)
                    continue;

                // Check if target is valid (not already symptomatic)
                PathogenicsMapComponent targetMapComponent = target.Map?.GetComponent<PathogenicsMapComponent>();
                if (targetMapComponent != null)
                {
                    PawnDiseaseState targetState = targetMapComponent.GetDiseaseState(target);
                    if (targetState != null && targetState.Stage >= SimulatedDiseaseStage.Symptomatic)
                        continue;
                }

                // Apply exposure
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
                "v0.3 uses hidden disease state (PawnDiseaseState).";

            Find.WindowStack.Add(new Dialog_MessageBox(text));
        }

        /// <summary>
        /// v0.3: Print current simulation state for all pawns on the map.
        /// </summary>
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

            if (mapComponent.PawnDiseaseStates.Count == 0)
            {
                Messages.Message("No pawns with disease state on this map.", MessageTypeDefOf.NeutralEvent, false);
                return;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Pathogenics Simulation State ===");
            sb.AppendLine($"Map ID: {map.uniqueID}");
            sb.AppendLine($"Pawns with disease state: {mapComponent.PawnDiseaseStates.Count}");
            sb.AppendLine();

            foreach (var kvp in mapComponent.PawnDiseaseStates)
            {
                Pawn pawn = FindPawnById(map, kvp.Key);
                PawnDiseaseState state = kvp.Value;

                string pawnName = pawn != null ? pawn.LabelShort : $"ID:{kvp.Key}";
                sb.AppendLine($"--- {pawnName} ---");
                sb.Append(state.GetDebugInfo(pawn));
                sb.AppendLine();
            }

            // Log to console
            DyzeLog.DevAction(sb.ToString());

            // Also show message
            Messages.Message("Simulation state logged to console (F12).", MessageTypeDefOf.PositiveEvent, false);
        }

        /// <summary>
        /// v0.3: Show detailed debug info for selected pawn in a message box.
        /// </summary>
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

            var mapComponent = pawn.Map?.GetComponent<PathogenicsMapComponent>();
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

            // Add infectiousness info
            float infectiousness = DiseaseImportationWorker.GetInfectiousnessForPawn(pawn);
            content += $"\nInfectiousness: {infectiousness:F2}";

            // Add recent exposure events if we had them (for now, just summary)
            if (diseaseState.Stage == SimulatedDiseaseStage.Exposed)
            {
                content += $"\nExposure: {diseaseState.Exposure:F2} / 1.00";
            }

            Find.WindowStack.Add(new Dialog_MessageBox(content, title));
        }

        /// <summary>
        /// Helper to find pawn by ID.
        /// </summary>
        private static Pawn FindPawnById(Map map, int pawnId)
        {
            var pawns = map.mapPawns.AllPawnsSpawned;
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i].thingIDNumber == pawnId)
                {
                    return pawns[i];
                }
            }
            return null;
        }

        private static PawnDiseaseState EnsureSymptomaticDiseaseState(Pawn pawn)
        {
            var mapComponent = pawn.Map?.GetComponent<PathogenicsMapComponent>();
            if (mapComponent == null)
            {
                return null;
            }

            int currentTick = Find.TickManager.TicksGame;
            PawnDiseaseState diseaseState = mapComponent.GetOrCreateDiseaseState(pawn);

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
